# Testare unitară în C# 

Am testat o componentă din aplicația Uniflow, și anume sistemul de gamificare care oferă utilizatorilor puncte XP în funcție de activitatea lor. Am vrut să verificăm dacă serviciul de gamificare calculează corect punctajul și dacă aplicația se comportă bine în situații diferite, inclusiv în cazuri limită.

## Clasa testată este GamificationService

Metoda CalculateXPFromVotes este responsabilă pentru calcularea numărului de puncte XP primite de un utilizator pentru o notiță, în funcție de voturile pozitive și negative primite din partea comunității.

Metoda primește două valori:
- upvotes - numărul de aprecieri ale notiței
- downvotes - numărul de dezaprecieri ale notiței

Sistemul calculează XP-ul pe baza mai multor reguli.

## Tabel XP pentru upvotes

| Interval upvotes | XP acordat |
|---|---|
| 0 - 10 | 10 XP / vot |
| 11 - 25 | 8 XP / vot |
| 26 - 50 | 5 XP / vot |
| 51 - 100 | 3 XP / vot |
| Peste 100 | 2 XP / vot |

Pentru voturile pozitive, XP-ul este acordat progresiv pe niveluri. Primele 10 upvotes oferă câte 10 XP pentru fiecare vot. Între 11 și 25 de upvotes se acordă câte 8 XP, între 26 și 50 câte 5 XP, între 51 și 100 câte 3 XP, iar pentru valorile peste 100 se acordă câte 2 XP pentru fiecare vot.

## Tabel penalizări pentru downvotes

| Interval downvotes | Penalizare |
|---|---|
| 0 - 5 | -2 XP / vot |
| 6 - 15 | -3 XP / vot |
| Peste 15 | -1 XP / vot |

Pentru voturile negative se aplică penalizări. Primele 5 downvotes scad câte 2 XP, valorile între 6 și 15 scad câte 3 XP, iar cele peste 15 scad câte 1 XP pentru fiecare vot.

## Tabel bonusuri de popularitate

| Prag upvotes | Bonus XP |
|---|---|
| 10 | +25 XP |
| 25 | +50 XP |
| 50 | +100 XP |
| 100 | +200 XP |

Pe lângă acestea, metoda oferă și bonusuri atunci când sunt atinse anumite praguri de popularitate:
- la 10 upvotes se acordă un bonus de 25 XP
- la 25 upvotes se acordă încă 50 XP
- la 50 upvotes se acordă încă 100 XP
- la 100 upvotes se acordă încă 200 XP

Rezultatul final reprezintă suma XP-ului obținut după aplicarea tuturor bonusurilor și penalizărilor.

---

# Funcționalitatea metodei

Metoda este împărțită în mai multe etape.
- calculează XP-ul pentru upvotes
- aplică penalizări pentru downvotes
- acordă bonusuri la pragurile de 10, 25, 50 și 100 upvotes
- verifică dacă rezultatul final este negativ

Dacă XP-ul final devine mai mic decât 0, metoda returnează 0.

Scopul sistemului este să recompenseze conținutul apreciat și să penalizeze notițele slab evaluate.

---

# Partiționarea în clase de echivalență

Pentru testare am împărțit datele de intrare în categorii similare (clase de echivalență).

## La upvotes:
- valori invalide
- intervalul 0-10
- intervalul 11-25
- intervalul 26-50
- intervalul 51-100
- valori peste 100

## La downvotes:
- valori invalide
- intervalul 0-5
- intervalul 6-15
- valori peste 15

Pentru fiecare categorie am ales valori reprezentative și am verificat dacă metoda returnează rezultatul corect.

---

# Analiza valorilor de frontieră

Am testat valorile aflate exact la limitele intervalelor pentru a evita erorile de tip off-by-one.

Exemple:
- 9, 10, 11
- 24, 25, 26
- 49, 50, 51
- 99, 100, 101
- 4, 5, 6
- 14, 15, 16

Aceste teste au confirmat că metoda aplică regulile corect la trecerea dintre praguri.

---

# Acoperirea la nivel de instrucțiune

Acoperirea la nivel de instrucțiune (Statement Coverage) verifică dacă fiecare bloc important din metodă a fost executat cel puțin o dată.

Primul scenariu a fost un caz de tip „Top Contributor”, cu:
- upvotes = 150

Acest test trece prin toate nivelurile de upvotes și activează toate bonusurile.

Al doilea scenariu a fost un caz de tip „Spammer”, cu:
- upvotes = 0
- downvotes = 100

Acest test verifică penalizările mari și cazul în care XP-ul final este plafonat la 0.

Prin aceste teste au fost executate toate instrucțiunile importante din metodă.

---

# Acoperirea la nivel de decizie

Acoperirea la nivel de decizie (Decision Coverage) verifică dacă fiecare ramură logică a fost parcursă.

Pentru fiecare condiție din cod am avut:
- un test care intră pe ramura „Da”
- un test care intră pe ramura „Nu”

Un exemplu important este verificarea:

```csharp
if(totalXP < 0)
```

Am testat:
- un caz în care XP-ul final este negativ și metoda returnează 0
- un caz în care XP-ul rămâne pozitiv

---

# Acoperirea la nivel de condiție

Acoperirea la nivel de condiție (Condition Coverage) verifică fiecare condiție logică separat.

În această metodă condițiile sunt simple și conțin un singur predicat (o singură verificare), de exemplu:

```csharp
upvotes <= 10
```

Din acest motiv, odată ce am obținut acoperirea la nivel de decizie, a fost acoperită automat și partea de condiții.

---

# Circuite independente și complexitate ciclomatică

Am realizat și graful de control al metodei (CFG - Control Flow Graph), adică diagrama care arată toate traseele posibile prin cod.

Pe baza acestuia am calculat complexitatea ciclomatică folosind formula lui McCabe.

Rezultatul obținut a fost:

```text
C = P + 1 = 11 + 1 = 12
```

- C = complexitatea ciclomatică
- P = numărul de decizii din cod

Acest lucru înseamnă că metoda are 12 circuite independente (trasee logice diferite).

Exemple de trasee:
- Caz simplu cu puține upvotes și puține downvotes
- Caz cu penalizare mare din cauza downvotes
- Caz cu multe upvotes și toate bonusurile activate

---

# Prompt Ai
Am o metoda C# numita CalculateXPFromVotes(int upvotes, int downvotes) care calculeaza XP-ul unui utilizator pe baza voturilor primite pentru o notita. Upvotes ofera XP progresiv pe 5 niveluri descrescatoare, downvotes aplica penalizari pe 3 niveluri, iar la pragurile de 10, 25, 50 si 100 upvotes se acorda bonusuri cumulative. Rezultatul nu poate fi niciodata negativ.

Creeaza un fisier C#, fara librarii externe, care sa contina logica metodei si un runner de teste care sa verifice clasele de echivalenta, valorile de frontiera la fiecare prag, cazurile in care XP-ul devine negativ, si in plus teste suplimentare care sa verifice fiecare milestone in izolare, combinatii de valori la frontiera simultane pentru upvotes si downvotes, valorile minime posibile (un singur vot) si comportamentul penalizarilor la valori mari de downvotes.

Fiecare test sa afiseze PASS sau FAIL cu valorile de intrare si rezultatul.

---

# Cod

```csharp
using System;

// ============================================================
//  Logica pură a metodei (fără dependențe de baza de date)
// ============================================================
class GamificationCalculator
{
    const int TIER1_UPVOTE_XP        = 10;
    const int TIER2_UPVOTE_XP        = 8;
    const int TIER3_UPVOTE_XP        = 5;
    const int TIER4_UPVOTE_XP        = 3;
    const int TIER5_UPVOTE_XP        = 2;

    const int TIER1_DOWNVOTE_PENALTY = 2;
    const int TIER2_DOWNVOTE_PENALTY = 3;
    const int TIER3_DOWNVOTE_PENALTY = 1;

    const int MILESTONE_10_BONUS     = 25;
    const int MILESTONE_25_BONUS     = 50;
    const int MILESTONE_50_BONUS     = 100;
    const int MILESTONE_100_BONUS    = 200;

    public int CalculateXPFromVotes(int upvotes, int downvotes)
    {
        int upvoteXP        = CalculateUpvoteXP(upvotes);
        int downvotePenalty = CalculateDownvotePenalty(downvotes);
        int milestoneBonuses = CalculateMilestoneBonuses(upvotes);
        int totalXP = upvoteXP - downvotePenalty + milestoneBonuses;
        return Math.Max(0, totalXP);
    }

    private int CalculateUpvoteXP(int upvoteCount)
    {
        int totalXP = 0;
        int tier1Count = Math.Min(upvoteCount, 10);
        totalXP += tier1Count * TIER1_UPVOTE_XP;
        if (upvoteCount <= 10) return totalXP;

        int tier2Count = Math.Min(upvoteCount - 10, 15);
        totalXP += tier2Count * TIER2_UPVOTE_XP;
        if (upvoteCount <= 25) return totalXP;

        int tier3Count = Math.Min(upvoteCount - 25, 25);
        totalXP += tier3Count * TIER3_UPVOTE_XP;
        if (upvoteCount <= 50) return totalXP;

        int tier4Count = Math.Min(upvoteCount - 50, 50);
        totalXP += tier4Count * TIER4_UPVOTE_XP;
        if (upvoteCount <= 100) return totalXP;

        int tier5Count = upvoteCount - 100;
        totalXP += tier5Count * TIER5_UPVOTE_XP;
        return totalXP;
    }

    private int CalculateDownvotePenalty(int downvoteCount)
    {
        int totalPenalty = 0;
        int tier1Count = Math.Min(downvoteCount, 5);
        totalPenalty += tier1Count * TIER1_DOWNVOTE_PENALTY;
        if (downvoteCount <= 5) return totalPenalty;

        int tier2Count = Math.Min(downvoteCount - 5, 10);
        totalPenalty += tier2Count * TIER2_DOWNVOTE_PENALTY;
        if (downvoteCount <= 15) return totalPenalty;

        int tier3Count = downvoteCount - 15;
        totalPenalty += tier3Count * TIER3_DOWNVOTE_PENALTY;
        return totalPenalty;
    }

    private int CalculateMilestoneBonuses(int upvoteCount)
    {
        int bonuses = 0;
        if (upvoteCount >= 100) bonuses += MILESTONE_100_BONUS;
        if (upvoteCount >= 50)  bonuses += MILESTONE_50_BONUS;
        if (upvoteCount >= 25)  bonuses += MILESTONE_25_BONUS;
        if (upvoteCount >= 10)  bonuses += MILESTONE_10_BONUS;
        return bonuses;
    }
}

// ============================================================
//  Runner de teste
// ============================================================
class TestRunner
{
    static int _passed = 0;
    static int _failed = 0;
    static readonly GamificationCalculator calc = new();

    static void Test(string label, int upvotes, int downvotes, int expected)
    {
        int actual = calc.CalculateXPFromVotes(upvotes, downvotes);
        if (actual == expected)
        {
            Console.WriteLine($"  ✅ PASS | {label,-45} | ({upvotes,4},{downvotes,3}) → {actual}");
            _passed++;
        }
        else
        {
            Console.WriteLine($"  ❌ FAIL | {label,-45} | ({upvotes,4},{downvotes,3}) → {actual} (așteptat {expected})");
            _failed++;
        }
    }

    static void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"┌─ {title} ─────────────────────────────────");
    }

    static void Main()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Teste CalculateXPFromVotes – GamificationService");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        // ── EP: Clase de Echivalență – Upvotes ──────────────────────
        Section("EP: Upvotes");
        Test("Input invalid (negativ) → 0",              -5,   0,   0);
        Test("Zero upvotes",                               0,   0,   0);
        Test("Tier 1 (5 upvotes)",                         5,   0,  50);
        Test("Tier 1 maxim (10 upvotes + bonus 25)",      10,   0, 125);
        Test("Tier 2 (15 upvotes)",                       15,   0, 165);
        Test("Tier 3 (30 upvotes)",                       30,   0, 320);
        Test("Tier 4 (60 upvotes)",                       60,   0, 550);
        Test("Tier 5 (110 upvotes)",                     110,   0, 890);

        // ── EP: Clase de Echivalență – Downvotes ────────────────────
        Section("EP: Downvotes");
        Test("Downvotes Tier 1 (3 downvotes)",            10,   3, 119);
        Test("Downvotes Tier 2 (10 downvotes)",           10,  10, 100);
        Test("Downvotes Tier 3 (20 downvotes)",           10,  20,  80);
        Test("Input invalid downvotes (negativ) → fara pen", 10, -3, 131);

        // ── BVA: Valori de Frontieră – Upvotes ──────────────────────
        Section("BVA: Frontiere Upvotes");
        Test("Sub pragul 10 (9 upvotes)",                  9,   0,  90);
        Test("LA pragul 10 + bonus",                      10,   0, 125);
        Test("Peste pragul 10 (11 upvotes)",              11,   0, 133);
        Test("Sub pragul 25 (24 upvotes)",                24,   0, 237);
        Test("LA pragul 25 + bonus",                      25,   0, 295);
        Test("Peste pragul 25 (26 upvotes)",              26,   0, 300);
        Test("Sub pragul 50 (49 upvotes)",                49,   0, 415);
        Test("LA pragul 50 + bonus",                      50,   0, 520);
        Test("Peste pragul 50 (51 upvotes)",              51,   0, 523);
        Test("Sub pragul 100 (99 upvotes)",               99,   0, 667);
        Test("LA pragul 100 + bonus",                    100,   0, 870);
        Test("Peste pragul 100 (101 upvotes)",           101,   0, 872);

        // ── BVA: Valori de Frontieră – Downvotes ────────────────────
        Section("BVA: Frontiere Downvotes");
        Test("Sub pragul 5 downvotes (4)",                10,   4, 117);
        Test("LA pragul 5 downvotes",                     10,   5, 115);
        Test("Peste pragul 5 (6 downvotes)",              10,   6, 112);
        Test("Sub pragul 15 downvotes (14)",              10,  14,  88);
        Test("LA pragul 15 downvotes",                    10,  15,  85);
        Test("Peste pragul 15 (16 downvotes)",            10,  16,  84);

        // ── Protecție: XP negativ plafonat la 0 ─────────────────────
        Section("Protectie: XP negativ → return 0");
        Test("Penalizare masiva, XP < 0 → 0",             2,  50,   0);
        Test("Zero upvotes, penalizare → 0",               0, 100,   0);

        // ── Teste Suplimentare: Milestones izolate ───────────────────
        // Verificăm că fiecare bonus apare EXACT o dată, nu acumulat greșit
        Section("Suplimentar: Verificare milestones individuale");
        // La exact 10 upvotes: 10*10 XP + 25 bonus = 125
        Test("Milestone 10 – doar bonusul de 25 activ",   10,   0, 125);
        // La exact 25 upvotes: 100 + 15*8 + 25+50 bonus = 295
        Test("Milestone 25 – bonusuri de 25+50 active",   25,   0, 295);
        // La exact 50 upvotes: 100+120+25*5 + 25+50+100 bonus = 520
        Test("Milestone 50 – bonusuri de 25+50+100",      50,   0, 520);
        // La exact 100 upvotes: toate tier-urile + toate bonusurile = 870
        Test("Milestone 100 – toate bonusurile active",  100,   0, 870);
        // La 9 upvotes: niciun milestone activ
        Test("Sub orice milestone – 0 bonusuri",           9,   0,  90);

        // ── Teste Suplimentare: Frontiere simultane up+down ──────────
        // Testăm combinații la limită pentru ambii parametri simultan
        Section("Suplimentar: Frontiere simultane upvotes + downvotes");
        // La exact 10 upvotes și 5 downvotes (ambele la prag): 125 - 10 = 115
        Test("Ambele la prag: up=10, down=5",             10,   5, 115);
        // La exact 25 upvotes și 15 downvotes: 295 - 40 = 255
        Test("Ambele la prag: up=25, down=15",            25,  15, 255);
        // La exact 100 upvotes și 15 downvotes: 870 - 40 = 830
        Test("Ambele la prag: up=100, down=15",          100,  15, 830);
        // Tier 5 upvotes + Tier 3 downvotes (cazul extrem pozitiv)
        Test("Extreme pozitiv: up=200, down=50",         200,  50, 995);

        // ── Teste Suplimentare: Un singur vot ────────────────────────
        Section("Suplimentar: Valori minime (un singur vot)");
        Test("Exact 1 upvote → 10 XP",                    1,   0,  10);
        Test("Exact 1 downvote → -2 XP → 0",              0,   1,   0);
        Test("1 upvote + 1 downvote → 10-2 = 8",          1,   1,   8);

        // ── Teste Suplimentare: Simetrie penalizare Tier 3 ───────────
        // Tier 3 downvotes = 1 XP/vot (mai mic decât Tier 1 și Tier 2)
        // Verificăm că la 100+ downvotes penalizarea nu crește la infinit
        Section("Suplimentar: Penalizare Tier 3 verificata la valori mari");
        // 10 up + 50 down: 125 - (10 + 30 + 35*1) = 125 - 75 = 50
        Test("50 downvotes cu 10 upvotes → 50 XP",       10,  50,  50);
        // 10 up + 100 down: 125 - (10 + 30 + 85) = 125 - 125 = 0
        Test("100 downvotes cu 10 upvotes → 0 XP",       10, 100,   0);

        // ── Sumar ────────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine($"  Rezultat: {_passed} PASSED  |  {_failed} FAILED  |  {_passed + _failed} total");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        if (_failed > 0)
            Environment.Exit(1); // exit code 1 dacă există eșecuri
    }
}

```

```
# Rezultat
═══════════════════════════════════════════════════════
  Teste CalculateXPFromVotes – GamificationService
═══════════════════════════════════════════════════════

┌─ EP: Upvotes ─────────────────────────────────
  PASS | Input invalid (negativ) → 0                   | (  -5,  0) → 0
  PASS | Zero upvotes                                  | (   0,  0) → 0
  PASS | Tier 1 (5 upvotes)                            | (   5,  0) → 50
  PASS | Tier 1 maxim (10 upvotes + bonus 25)          | (  10,  0) → 125
  PASS | Tier 2 (15 upvotes)                           | (  15,  0) → 165
  PASS | Tier 3 (30 upvotes)                           | (  30,  0) → 320
  PASS | Tier 4 (60 upvotes)                           | (  60,  0) → 550
  PASS | Tier 5 (110 upvotes)                          | ( 110,  0) → 890

┌─ EP: Downvotes ─────────────────────────────────
  PASS | Downvotes Tier 1 (3 downvotes)                | (  10,  3) → 119
  PASS | Downvotes Tier 2 (10 downvotes)               | (  10, 10) → 100
  PASS | Downvotes Tier 3 (20 downvotes)               | (  10, 20) → 80
  PASS | Input invalid downvotes (negativ) → fara pen  | (  10, -3) → 131

┌─ BVA: Frontiere Upvotes ─────────────────────────────────
  PASS | Sub pragul 10 (9 upvotes)                     | (   9,  0) → 90
  PASS | LA pragul 10 + bonus                          | (  10,  0) → 125
  PASS | Peste pragul 10 (11 upvotes)                  | (  11,  0) → 133
  PASS | Sub pragul 25 (24 upvotes)                    | (  24,  0) → 237
  PASS | LA pragul 25 + bonus                          | (  25,  0) → 295
  PASS | Peste pragul 25 (26 upvotes)                  | (  26,  0) → 300
  PASS | Sub pragul 50 (49 upvotes)                    | (  49,  0) → 415
  PASS | LA pragul 50 + bonus                          | (  50,  0) → 520
  PASS | Peste pragul 50 (51 upvotes)                  | (  51,  0) → 523
  PASS | Sub pragul 100 (99 upvotes)                   | (  99,  0) → 667
  PASS | LA pragul 100 + bonus                         | ( 100,  0) → 870
  PASS | Peste pragul 100 (101 upvotes)                | ( 101,  0) → 872

┌─ BVA: Frontiere Downvotes ─────────────────────────────────
  PASS | Sub pragul 5 downvotes (4)                    | (  10,  4) → 117
  PASS | LA pragul 5 downvotes                         | (  10,  5) → 115
  PASS | Peste pragul 5 (6 downvotes)                  | (  10,  6) → 112
  PASS | Sub pragul 15 downvotes (14)                  | (  10, 14) → 88
  PASS | LA pragul 15 downvotes                        | (  10, 15) → 85
  PASS | Peste pragul 15 (16 downvotes)                | (  10, 16) → 84

┌─ Protectie: XP negativ → return 0 ─────────────────────────────────
  PASS | Penalizare masiva, XP < 0 → 0                 | (   2, 50) → 0
  PASS | Zero upvotes, penalizare → 0                  | (   0,100) → 0

═══════════════════════════════════════════════════════
  Rezultat: 32 PASSED  |  0 FAILED  |  32 total
═══════════════════════════════════════════════════════
```
