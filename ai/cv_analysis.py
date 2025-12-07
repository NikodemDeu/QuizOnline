import logging
import json
import re
import unicodedata
from typing import List, Dict
from typing import Dict
from openai import AsyncOpenAI
from app.config import settings
from app.ai.prompts import CV_ANALYSIS_SYSTEM_PROMPT, CV_REANALYSIS_SYSTEM_PROMPT


def clean_text(text: str) -> str:
    """
    Czyści tekst z problematycznych znaków Unicode
    """
    if not text:
        return ''
    
    try:
        # 1. Usuń surrogate pairs
        text = text.encode('utf-8', errors='ignore').decode('utf-8', errors='ignore')
        
        # 2. Normalizacja Unicode
        text = unicodedata.normalize('NFKC', text)
        
        # 3. Usuń control characters (oprócz \n, \r, \t)
        text = ''.join(char for char in text if unicodedata.category(char)[0] != 'C' or char in '\n\r\t')
        
        # 4. Usuń replacement character
        text = text.replace('\ufffd', '')
        text = text.replace('\u0000', '')
        
        # 5. Usuń nadmiarowe whitespace
        lines = text.split('\n')
        cleaned_lines = [' '.join(line.split()) for line in lines]
        text = '\n'.join(cleaned_lines)
        
        # 6. Usuń puste linie
        text = '\n'.join(line for line in text.split('\n') if line.strip())
        
        return text.strip()
        
    except Exception as e:
        logger.error(f"Błąd czyszczenia tekstu: {e}")
        return text.encode('ascii', errors='ignore').decode('ascii')

logger = logging.getLogger(__name__)

client = AsyncOpenAI(api_key=settings.OPENAI_API_KEY)


def extract_json(text: str) -> str:
    """Wyciąga czysty JSON z odpowiedzi AI"""
    try:
        cleaned = text.replace('```json', '').replace('```', '').strip()
        
        first_brace = cleaned.find('{')
        last_brace = cleaned.rfind('}')
        
        if first_brace == -1 or last_brace == -1:
            raise ValueError('Brak { lub } w odpowiedzi')
        
        cleaned = cleaned[first_brace:last_brace + 1]
        json.loads(cleaned)  # Walidacja
        
        return cleaned
        
    except Exception as e:
        match = re.search(r'\{[\s\S]*\}', text)
        if match:
            return match.group(0)
        
        logger.error(f"Nie można wyodrębnić JSON: {e}")
        raise ValueError('Nie można wyodrębnić JSON z odpowiedzi AI')


async def analyze_cv(job_offer: str, cv_text: str) -> Dict:
    """Główna funkcja analizy CV"""
    try:
        logger.info("Rozpoczynam analizę CV...")
        
        response = await client.chat.completions.create(
            model=settings.CV_ANALYZER_MODEL,
            messages=[
                {"role": "system", "content": CV_ANALYSIS_SYSTEM_PROMPT},
                {"role": "user", "content": f"OFERTA PRACY:\n{job_offer}\n\n---\n\nCV KANDYDATA:\n{cv_text}"}
            ],
            temperature=0.0,
            max_tokens=1500,
            seed=42
        )
        
        raw_content = response.choices[0].message.content
        logger.debug(f"Raw AI response: {raw_content[:200]}...")
        
        cleaned_json = extract_json(raw_content)
        analysis = json.loads(cleaned_json)
        
        # Walidacja
        analysis.setdefault('fit_score', 0)
        analysis.setdefault('decision', 'maybe')
        analysis.setdefault('matches', [])
        analysis.setdefault('gaps', [])
        analysis.setdefault('red_flags', [])
        analysis.setdefault('questions_to_candidate', [])
        analysis.setdefault('summary_md', 'Brak podsumowania')
        
        logger.info(f"Analiza zakończona. Score: {analysis['fit_score']}%")
        
        return analysis
        
    except Exception as e:
        logger.error(f"Błąd podczas analizy CV: {e}")
        raise


async def reanalyze_with_answers(job_offer: str, cv_text: str, answers: Dict[str, str]) -> Dict:
    """Re-analiza CV po uzupełnieniu pytań"""
    try:
        logger.info(f"Re-analiza z {len(answers)} odpowiedziami...")
        
        answers_text = '\n\n=== UZUPEŁNIENIA OD KANDYDATA ===\n'
        for question, answer in answers.items():
            answers_text += f"Pytanie: {question}\nOdpowiedź: {answer}\n\n"
        
        enhanced_cv = cv_text + answers_text
        
        response = await client.chat.completions.create(
            model=settings.CV_ANALYZER_MODEL,
            messages=[
                {"role": "system", "content": CV_REANALYSIS_SYSTEM_PROMPT},
                {"role": "user", "content": f"OFERTA PRACY:\n{job_offer}\n\n---\n\nCV KANDYDATA:\n{enhanced_cv}"}
            ],
            temperature=0.0,
            max_tokens=1500,
            seed=42
        )
        
        raw_content = response.choices[0].message.content
        cleaned_json = extract_json(raw_content)
        analysis = json.loads(cleaned_json)
        
        logger.info(f"Re-analiza zakończona. Nowy score: {analysis['fit_score']}%")
        
        return analysis
        
    except Exception as e:
        logger.error(f"Błąd re-analizy: {e}")
        raise


async def match_cv_to_job_weighted(
    cv_text: str,
    job_requirements: List[Dict],
    job_description: str = ""
) -> Dict:
    """
    Uniwersalny matching CV do oferty - działa dla KAŻDEJ branży
    
    Args:
        cv_text: Tekst CV kandydata
        job_requirements: Lista wymagań z wagami
            [{"text": "Python 3+ lata", "weight": "CRITICAL"}, ...]
        job_description: Opcjonalny opis oferty
        
    Returns:
        {
            "base_score": 85,
            "bonus_score": 5,
            "final_score": 90,
            "decision": "proceed",
            ...
        }
    """
    try:
        from datetime import datetime
        from app.ai.prompts import CV_MATCHING_UNIVERSAL_PROMPT
        
        logger.info("Starting weighted CV matching...")
        
        # Wyczyść teksty
        cv_text = clean_text(cv_text)
        job_description = clean_text(job_description)
        
        # Zbuduj opis wymagań
        requirements_text = "\n".join([
            f"• {req['text']} [{req['weight']}]"
            for req in job_requirements
        ])
        
        # Dzisiejsza data
        current_date = datetime.now().strftime("%Y-%m-%d")
        
        # System prompt
        system_prompt = CV_MATCHING_UNIVERSAL_PROMPT.replace("{current_date}", current_date)
        
        # User prompt
        user_prompt = f"""OFERTA PRACY:
{job_description if job_description else "Brak dodatkowego opisu"}

WYMAGANIA (z wagami):
{requirements_text}

---

CV KANDYDATA:
{cv_text}

Przeanalizuj i zwróć JSON z oceną."""
        
        # Wywołanie OpenAI
        response = await client.chat.completions.create(
            model=settings.CV_ANALYZER_MODEL,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ],
            temperature=0.0,
            max_tokens=2500,
            seed=42
        )
        
        raw_content = response.choices[0].message.content
        logger.debug(f"Raw AI response: {raw_content[:200]}...")
        
        # Parse JSON
        cleaned_json = extract_json(raw_content)
        analysis = json.loads(cleaned_json)
        
        # Walidacja
        analysis.setdefault('base_score', 0)
        analysis.setdefault('bonus_score', 0)
        analysis.setdefault('final_score', 0)
        analysis.setdefault('decision', 'maybe')
        analysis.setdefault('matches', [])
        analysis.setdefault('gaps', [])
        analysis.setdefault('red_flags', [])
        analysis.setdefault('questions_to_candidate', [])
        analysis.setdefault('summary_md', '')
        analysis.setdefault('requirements_breakdown', [])
        analysis.setdefault('bonus_breakdown', [])
        
        logger.info(f"Matching complete. Base: {analysis['base_score']}%, Final: {analysis['final_score']}%")
        
        return analysis
        
    except Exception as e:
        logger.error(f"Error in weighted matching: {e}")
        raise


async def reanalyze_weighted_with_answers(
    cv_text: str,
    job_requirements: List[Dict],
    answers: Dict[str, str],
    job_description: str = ""
) -> Dict:
    """
    Re-analiza po uzupełnieniu pytań (z wagami)
    """
    try:
        logger.info(f"Re-analyzing with {len(answers)} answers...")
        
        # Wyczyść
        cv_text = clean_text(cv_text)
        
        # Dodaj odpowiedzi
        answers_text = '\n\n=== UZUPEŁNIENIA OD KANDYDATA ===\n'
        for question, answer in answers.items():
            question_clean = clean_text(question)
            answer_clean = clean_text(answer)
            answers_text += f"Q: {question_clean}\nA: {answer_clean}\n\n"
        
        enhanced_cv = cv_text + answers_text
        
        # Użyj tej samej funkcji
        return await match_cv_to_job_weighted(
            enhanced_cv,
            job_requirements,
            job_description
        )
        
    except Exception as e:
        logger.error(f"Error in re-analysis: {e}")
        raise