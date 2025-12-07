import logging
import json
from typing import Dict, List
from datetime import datetime
from openai import AsyncOpenAI
from app.config import settings
from app.ai.prompts import QUESTION_GENERATION_PROMPT

logger = logging.getLogger(__name__)
client = AsyncOpenAI(api_key=settings.OPENAI_API_KEY)


async def generate_followup_questions(
    cv_text: str,
    job_requirements: List[Dict],
    gaps: List[str],
    max_questions: int = 5
) -> List[Dict]:
    """
    Generuje pytania uzupełniające na podstawie braków w CV
    
    Args:
        cv_text: Tekst CV kandydata
        job_requirements: Lista wymagań z wagami
        gaps: Lista zidentyfikowanych braków
        max_questions: Maksymalna liczba pytań (domyślnie 5)
        
    Returns:
        Lista pytań:
        [
            {
                "question": "Czy masz doświadczenie z Django?",
                "type": "text|yes_no|single_choice|number",
                "priority": "critical|high|medium|low",
                "reason": "Django wymienione jako KONIECZNE",
                "options": ["A1", "A2", "B1", "B2"]  # tylko dla single_choice
            }
        ]
    """
    try:
        logger.info(f"🔄 Generowanie pytań dla {len(gaps)} braków...")
        
        # Zbuduj kontekst wymagań
        requirements_text = "\n".join([
            f"• {req['text']} [{req['weight']}]"
            for req in job_requirements
        ])
        
        # Zbuduj kontekst braków
        gaps_text = "\n".join([f"• {gap}" for gap in gaps])
        
        # Dzisiejsza data
        current_date = datetime.now().strftime("%Y-%m-%d")
        
        # System prompt
        system_prompt = QUESTION_GENERATION_PROMPT.replace("{current_date}", current_date)
        
        # User prompt
        user_prompt = f"""WYMAGANIA OFERTY:
{requirements_text}

ZIDENTYFIKOWANE BRAKI W CV:
{gaps_text}

CV KANDYDATA (fragment):
{cv_text[:1500]}

Wygeneruj maksymalnie {max_questions} najważniejszych pytań uzupełniających.
Pytania powinny dotyczyć braków KONIECZNYCH i BARDZO_WAZNYCH.

Zwróć TYLKO JSON z listą pytań."""
        
        # Wywołanie AI
        response = await client.chat.completions.create(
            model=settings.CV_ANALYZER_MODEL,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ],
            temperature=0.3,
            max_tokens=1500,
            seed=42
        )
        
        raw_content = response.choices[0].message.content
        logger.debug(f"Raw AI response: {raw_content[:200]}...")
        
        # Parse JSON
        cleaned = raw_content.strip()
        if cleaned.startswith("```json"):
            cleaned = cleaned[7:]
        if cleaned.startswith("```"):
            cleaned = cleaned[3:]
        if cleaned.endswith("```"):
            cleaned = cleaned[:-3]
        cleaned = cleaned.strip()
        
        result = json.loads(cleaned)
        
        # Wyciągnij pytania (może być w różnych formatach)
        if isinstance(result, dict) and 'questions' in result:
            questions = result['questions']
        elif isinstance(result, list):
            questions = result
        else:
            logger.error(f"Unexpected format: {result}")
            return []
        
        # Walidacja pytań
        validated = []
        for q in questions[:max_questions]:
            if isinstance(q, dict) and 'question' in q:
                validated.append({
                    'question': q.get('question', ''),
                    'type': q.get('type', 'text'),
                    'priority': q.get('priority', 'medium'),
                    'reason': q.get('reason', ''),
                    'options': q.get('options', [])
                })
        
        logger.info(f"✅ Wygenerowano {len(validated)} pytań")
        
        return validated
        
    except Exception as e:
        logger.error(f"❌ Błąd generowania pytań: {e}")
        return []


async def regenerate_questions(
    cv_text: str,
    job_requirements: List[Dict],
    previous_answers: Dict[str, str],
    max_questions: int = 5
) -> List[Dict]:
    """
    Regeneruje pytania po otrzymaniu odpowiedzi
    (jeśli nadal są braki)
    """
    try:
        logger.info("🔄 Regenerowanie pytań po odpowiedziach...")
        
        # Dodaj odpowiedzi do CV
        answers_text = "\n\n=== UZUPEŁNIENIA OD KANDYDATA ===\n"
        for question, answer in previous_answers.items():
            answers_text += f"Q: {question}\nA: {answer}\n\n"
        
        enhanced_cv = cv_text + answers_text
        
        # Re-analiza z uzupełnionym CV
        from app.ai.cv_matching import match_cv_to_job_weighted
        
        analysis = await match_cv_to_job_weighted(
            cv_text=enhanced_cv,
            job_requirements=job_requirements
        )
        
        # Jeśli nadal są braki - wygeneruj nowe pytania
        if analysis.get('gaps'):
            return await generate_followup_questions(
                cv_text=enhanced_cv,
                job_requirements=job_requirements,
                gaps=analysis['gaps'],
                max_questions=max_questions
            )
        
        return []
        
    except Exception as e:
        logger.error(f"❌ Błąd regeneracji pytań: {e}")
        return []