Dokumentacja Techniczna - Aplikacja Quiz Online

Tytuł projektu: Quiz Online
Autor projektu: Nikodem Deutsch

Opis działania:

Quiz Online to aplikacja webowa przeznaczona do tworzenia i rozwiązywania interaktywnych quizów. Każdy użytkownik po stworzeniu konta otrzymuje dostęp do prywatnej przestrzeni, w której może tworzyć własne quizy, zarządzać ich treścią, a także śledzić szczegółowe statystyki dotyczące popularności pytań i wyników. Aplikacja zapewnia pełną personalizację i bezpieczeństwo danych – użytkownicy mają dostęp wyłącznie do własnych treści i wyników.

Specyfikacja wykorzystanych technologii

Framework: ASP.NET Core 8
Wzorzec architektoniczny: Model-View-Controller (MVC)
Baza danych: SQLite
Mapowanie Obiektowo-Relacyjne (ORM): Entity Framework Core 8
System użytkowników: Microsoft Identity
Frontend: HTML, CSS, Bootstrap (domyślny z szablonu)

 Instrukcje pierwszego uruchomienia projektu

1.  Pobranie repozytorium: wystarczy wejść w  link: https://github.com/NikodemDeu/QuizOnline i pobrać zawartość.
2.  Otwarcie projektu: Otwórz plik solucji (`.sln`) w programie Visual Studio 2022 lub nowszym   
3. Uruchomienie: Uruchom projekt z poziomu Visual Studio (przycisk "Play" lub klawisz `F5`). 

 Opis struktury projektu
Models: Definiują strukturę danych aplikacji (`Quiz`, `Question`, `Answer` itp.). Są to klasy C#, które Entity Framework mapuje na tabele w bazie danych. Zawierają również logikę walidacji (np. `[Required]`).
Views: Pliki `.cshtml` odpowiedzialne za generowanie interfejsu użytkownika (HTML). Każdy kontroler posiada swój dedykowany podfolder w `Views`.
Controllers: Pośredniczą między modelami a widokami. Przetwarzają żądania HTTP od użytkownika, wykonują operacje na danych za pomocą `DbContext` i zwracają odpowiedni widok z danymi.
ViewModels: Dodatkowy folder zawierający specjalne modele (`QuizDetailsViewModel`, `AttemptDetailsViewModel`), które służą do "pakowania" i przekazywania złożonych danych z kontrolera do konkretnego widoku.
Data:  Zawiera klasę `ApplicationDbContext` (kontekst bazy danych EF Core) oraz folder `Migrations` z historią zmian w schemacie bazy danych.

Wylistowane modele

 `Quiz`
Reprezentuje pojedynczy quiz stworzony przez użytkownika.
•	`Id` (int): Klucz główny.
•	`Title` (string): Tytuł quizu. Ograniczenia: Wymagany (`[Required]`), długość od 3 do 150 znaków (`[StringLength]`).
•	`Description` (string?): Opcjonalny opis quizu.
•	`UserId` (string): Klucz obcy do tabeli użytkowników (`AspNetUsers`), identyfikuje twórcę. Ograniczenia: Wymagany.
•	`Questions` (ICollection<Question>): Relacja "jeden do wielu" z pytaniami.

`Question`
Reprezentuje pojedyncze pytanie w ramach quizu.
•	`Id` (int): Klucz główny.
•	`Text` (string): Treść pytania. Ograniczenia: Wymagany (`[Required]`).
•	`Points` (int): Liczba punktów za poprawną odpowiedź. Ograniczenia: Wartość od 1 do 100 (`[Range]`), domyślnie 1.
•	`QuizId` (int): Klucz obcy do tabeli `Quizzes`.
•	`Answers` (ICollection<Answer>): Relacja "jeden do wielu" z odpowiedziami.

`Answer`
Reprezentuje pojedynczą opcję odpowiedzi na pytanie.
•	`Id` (int): Klucz główny.
•	`Text` (string): Treść odpowiedzi. Ograniczenia: Wymagany (`[Required]`).
•	`IsCorrect` (bool): Flaga wskazująca, czy odpowiedź jest poprawna.
•	`QuestionId` (int): Klucz obcy do tabeli `Questions`.

`QuizAttempt`
Reprezentuje pojedynczą próbę rozwiązania quizu przez użytkownika.
•	`Id` (int): Klucz główny.
•	`QuizId` (int): Klucz obcy do quizu, który był rozwiązywany.
•	`UserId` (string): Klucz obcy do użytkownika, który rozwiązywał quiz.
•	`StartTime` (DateTime): Czas rozpoczęcia.
•	`EndTime` (DateTime?): Czas zakończenia.
•	`Score` (int): Wynik końcowy w procentach. Ograniczenia: Wartość od 0 do 100 (`[Range]`).
•	`AttemptAnswers` (ICollection<AttemptAnswer>): Relacja do wybranych odpowiedzi.

`AttemptAnswer`
Tabela łącząca, która przechowuje informację, którą konkretnie odpowiedź wybrał użytkownik w danym podejściu.
•	`Id` (int): Klucz główny.
•	`QuizAttemptId` (int): Klucz obcy do próby (`QuizAttempt`).
•	`SelectedAnswerId` (int): Klucz obcy do wybranej odpowiedzi (`Answer`).

Wylistowane kontrolery

`QuizzesController`
•	Odpowiedzialny za zarządzanie (CRUD) quizami przez zalogowanego użytkownika.
•	`Index()` [GET]: Wyświetla listę quizów należących tylko do zalogowanego użytkownika.
•	`Details(int? id)` [GET]: Wyświetla szczegóły quizu wraz z jego pytaniami, odpowiedziami oraz agregowanymi statystykami (liczba podejść, średni wynik, popularność odpowiedzi). Sprawdza, czy użytkownik jest właścicielem quizu.
•	`Create()` [GET]: Wyświetla formularz tworzenia nowego quizu.
•	`Create(Quiz quiz)` [POST]: Przetwarza formularz, przypisuje `UserId` zalogowanego użytkownika i zapisuje nowy quiz w bazie.
•	`AddQuestion(Question question)` [POST]: Dodaje nowe pytanie do quizu.
•	`AddAnswer(Answer answer)` [POST]: Dodaje nową odpowiedź do pytania.
•	`Edit(int? id)` / `Edit(int id, Quiz quiz)` [GET/POST]: Edycja quizu, z weryfikacją uprawnień.
•	`Delete(int? id)` / `DeleteConfirmed(int id)` [GET/POST]: Usuwanie quizu, z weryfikacją uprawnień.

`QuizTakingController`
Odpowiedzialny za proces rozwiązywania quizu.
•	`Start(int id)` [GET]: Wyświetla stronę z formularzem quizu (pytania i opcje odpowiedzi).
•	`SubmitQuiz(int quizId, ...)` [POST]: Przyjmuje odpowiedzi z formularza, oblicza wynik na podstawie punktów, zapisuje próbę (`QuizAttempt`) i wybrane odpowiedzi (`AttemptAnswers`) w bazie danych, a następnie przekierowuje do strony z wynikami.

`StatisticsController`
Odpowiedzialny za wyświetlanie wyników użytkownika.

•	`Index()` [GET]: Wyświetla historię wszystkich prób rozwiązania quizów przez zalogowanego użytkownika.
•	`AttemptDetails(int id)` [GET]: Wyświetla szczegółową analizę jednej, konkretnej próby: wynik procentowy i punktowy, listę pytań z zaznaczeniem, które odpowiedzi były poprawne, a które błędne.

`HomeController`
Odpowiedzialny za strony ogólne.
   `Index()` [GET]: Wyświetla stronę powitalną dla gości. Jeśli użytkownik jest zalogowany, automatycznie przekierowuje go do `/Quizzes`.
   `Error(...)`: Obsługuje błędy aplikacji, w tym wyświetla dedykowaną stronę dla błędu 404.

Opis systemu użytkowników

System oparty jest na Microsoft Identity. W obecnej wersji nie ma zdefiniowanych ról (np. Administrator) – istnieje tylko podział na gości i użytkowników zalogowanych.

   Goście (niezalogowani): Mogą zobaczyć tylko stronę główną z zachętą do rejestracji/logowania. Próba dostępu do jakiejkolwiek innej funkcjonalności (np. `/Quizzes`) skutkuje przekierowaniem na stronę logowania.
   Użytkownicy zalogowani: Mają pełen dostęp do funkcjonalności aplikacji: tworzenia, zarządzania i rozwiązywania quizów oraz przeglądania swoich wyników.
   Powiązanie danych: Informacje są ściśle powiązane z konkretnym użytkownikiem. Modele `Quiz` i `QuizAttempt` przechowują `UserId`. Logika w kontrolerach (filtrowanie `.Where(x => x.UserId == ...)` i weryfikacja uprawnień) gwarantuje, że użytkownik ma dostęp tylko do własnych danych. Nie ma żadnych informacji "globalnych" widocznych dla wszystkich.

Krótka charakterystyka najciekawszych funkcjonalności

1.  Dynamiczne Statystyki Popularności Odpowiedzi: Aplikacja w czasie rzeczywistym oblicza i prezentuje twórcy quizu, jak popularne są poszczególne opcje odpowiedzi. Zapytanie do bazy danych wykorzystuje grupowanie (`GroupBy`) i zliczanie (`Count`) rekordów w tabeli `AttemptAnswers`, aby dynamicznie generować procentowy rozkład wyborów dla każdego pytania. Pozwala to autorowi na analizę i ulepszanie swoich quizów.

2.  Szczegółowa Analiza Wyniku po Zakończeniu Quizu: Po rozwiązaniu quizu użytkownik nie widzi tylko suchego wyniku, ale otrzymuje szczegółowe podsumowanie. Aplikacja prezentuje każdą odpowiedź, wizualnie oznaczając ją jako poprawną (kolor zielony) lub błędną (kolor czerwony), a w przypadku pomyłki wskazuje również prawidłową opcję. Dodatkowo, przy każdym pytaniu widoczna jest liczba zdobytych punktów w stosunku do maksymalnej możliwej, co daje pełen obraz postępów.

3.  Pełna Personalizacja i Bezpieczeństwo Danych: Architektura aplikacji opiera się na ścisłej izolacji danych użytkowników. Każdy quiz i każda próba jego rozwiązania są nierozerwalnie związane z identyfikatorem użytkownika. Dzięki zastosowaniu filtrowania po stronie serwera we wszystkich kluczowych zapytaniach, aplikacja gwarantuje, że użytkownicy nigdy nie zobaczą treści ani wyników nienależących do nich, co zapewnia prywatność i bezpieczeństwo.
