# Testare unitară în C#

S-a testat o componentă din aplicația Uniflow, și anume sistemul de gamificare care oferă utilizatorilor puncte XP în funcție de activitatea lor. S-a urmărit să se verifice dacă serviciul de gamificare calculează corect punctajul și dacă aplicația se comportă bine în situații diferite, inclusiv în cazuri limită.

## Clasa testată este GamificationService

Metoda `CalculateXPFromVotes` este responsabilă pentru calcularea numărului de puncte XP primite de un utilizator pentru o notiță, în funcție de voturile pozitive și negative primite din partea comunității.

Metoda primește două valori:
- **upvotes** - numărul de aprecieri ale notiței
- **downvotes** - numărul de dezaprecieri ale notiței

Sistemul calculează XP-ul pe baza mai multor reguli.

### Tabel XP pentru upvotes

| Interval upvotes | XP acordat |
|------------------|------------|
| 0 - 10           | 10 XP / vot|
| 11 - 25          | 8 XP / vot |
| 26 - 50          | 5 XP / vot |
| 51 - 100         | 3 XP / vot |
| Peste 100        | 2 XP / vot |

Pentru voturile pozitive, XP-ul este acordat progresiv pe niveluri. Primele 10 upvotes oferă câte 10 XP pentru fiecare vot. Între 11 și 25 de upvotes se acordă câte 8 XP, între 26 și 50 câte 5 XP, între 51 și 100 câte 3 XP, iar pentru valorile peste 100 se acordă câte 2 XP pentru fiecare vot.

### Tabel penalizări pentru downvotes

| Interval downvotes | Penalizare |
|--------------------|------------|
| 0 - 5              | -2 XP / vot|
| 6 - 15             | -3 XP / vot|
| Peste 15           | -1 XP / vot|

Pentru voturile negative se aplică penalizări. Primele 5 downvotes scad câte 2 XP, valorile între 6 și 15 scad câte 3 XP, iar cele peste 15 scad câte 1 XP pentru fiecare vot.

### Tabel bonusuri de popularitate

| Prag upvotes | Bonus XP |
|--------------|----------|
| 10           | +25 XP   |
| 25           | +50 XP   |
| 50           | +100 XP  |
| 100          | +200 XP  |

Pe lângă acestea, metoda oferă și bonusuri atunci când sunt atinse anumite praguri de popularitate:
- la 10 upvotes se acordă un bonus de 25 XP
- la 25 upvotes se acordă încă 50 XP
- la 50 upvotes se acordă încă 100 XP
- la 100 upvotes se acordă încă 200 XP

Rezultatul final reprezintă suma XP-ului obținut după aplicarea tuturor bonusurilor și penalizărilor.

### Funcționalitatea metodei

Metoda este împărțită în mai multe etape:
1. calculează XP-ul pentru upvotes
2. aplică penalizări pentru downvotes
3. acordă bonusuri la pragurile de 10, 25, 50 și 100 upvotes
4. verifică dacă rezultatul final este negativ

Dacă XP-ul final devine mai mic decât 0, metoda returnează 0.
Scopul sistemului este să recompenseze conținutul apreciat și să penalizeze notițele slab evaluate.

---

## Partiționarea în clase de echivalență

Pentru testare, datele de intrare au fost împărțite în categorii similare (clase de echivalență).

**Clase individuale la upvotes (U):**

| Clasă | Interval | Reprezentant |
|---|---|---|
| U_1 (invalid) | upvotes < 0 | -5 |
| U_2 | upvotes = 0 | 0 |
| U_3 | 1 <= upvotes <= 10 | 5 |
| U_4 | 11 <= upvotes <= 25 | 15 |
| U_5 | 26 <= upvotes <= 50 | 30 |
| U_6 | 51 <= upvotes <= 100 | 60 |
| U_7 | upvotes > 100 | 110 |

**Clase individuale la downvotes (D):**

| Clasă | Interval | Reprezentant |
|---|---|---|
| D_1 (invalid) | downvotes < 0 | -3 |
| D_2 | downvotes = 0 | 0 |
| D_3 | 1 <= downvotes <= 5 | 3 |
| D_4 | 6 <= downvotes <= 15 | 10 |
| D_5 | downvotes > 15 | 20 |

**Clase globale**
Clasele globale se obțin prin combinarea claselor individuale:

| Clasă globală | Upvotes (U) | Downvotes (D) | Reprezentant |
|---|---|---|---|
| C_11 | U_2 (0) | D_2 (0) | (0, 0) |
| C_32 | U_3 (1–10) | D_2 (0) | (5, 0) |
| C_33 | U_3 (1–10) | D_3 (1–5) | (10, 3) |
| C_34 | U_3 (1–10) | D_4 (6–15) | (10, 10) |
| C_35 | U_3 (1–10) | D_5 (>15) | (10, 20) |
| C_42 | U_4 (11–25) | D_2 (0) | (15, 0) |
| C_52 | U_5 (26–50) | D_2 (0) | (30, 0) |
| C_62 | U_6 (51–100) | D_2 (0) | (60, 0) |
| C_72 | U_7 (>100) | D_2 (0) | (110, 0) |
| C_U1 | U_1 (invalid) | — | (-5, 0) |
| C_D1 | — | D_1 (invalid) | (10, -3) |

Pentru fiecare categorie au fost alese valori reprezentative și s-a verificat dacă metoda returnează rezultatul corect. Toate aceste teste se regăsesc și în suita xUnit (`GamificationTests.cs`).

---

## Analiza valorilor de frontieră (BVA)

Au fost testate valorile aflate exact la limitele intervalelor pentru a evita erorile de tip off-by-one.

**Frontierele testate pentru upvotes (U):**
* U_2/U_3 (prag 0): 0, 1
* U_3/U_4 (prag 10): 9, 10, 11
* U_4/U_5 (prag 25): 24, 25, 26
* U_5/U_6 (prag 50): 49, 50, 51
* U_6/U_7 (prag 100): 99, 100, 101

**Frontierele testate pentru downvotes (D):**
* D_3/D_4 (prag 5): 4, 5, 6
* D_4/D_5 (prag 15): 14, 15, 16

Aceste teste au confirmat că metoda aplică regulile corect la trecerea dintre praguri. 
Pentru a aprofunda analiza (BVA pe clase globale), au fost testate inclusiv frontiere simultane, adică punctele în care ambele variabile iau valori limită în același timp:

| Clasă globală | upvotes | downvotes | XP așteptat |
|---|---|---|---|
| C_33 | 10 | 5 | 115 |
| C_34 | 10 | 15 | 85 |
| C_42 | 25 | 5 | 285 |
| C_42 | 25 | 15 | 255 |
| C_52 | 50 | 5 | 510 |
| C_62 | 100 | 15 | 830 |

---

## Acoperirea Testelor (Coverage)

### Acoperirea la nivel de instrucțiune
Acoperirea la nivel de instrucțiune (Statement Coverage) verifică dacă fiecare bloc important din metodă a fost executat cel puțin o dată.

* Primul scenariu a fost un caz de tip „Top Contributor”, cu: `upvotes = 150`
Acest test trece prin toate nivelurile de upvotes și activează toate bonusurile.

* Al doilea scenariu a fost un caz de tip „Spammer”, cu: `upvotes = 0`, `downvotes = 100`
Acest test verifică penalizările mari și cazul în care XP-ul final este plafonat la 0.

Prin aceste teste au fost executate toate instrucțiunile importante din metodă.

### Acoperirea la nivel de decizie
Acoperirea la nivel de decizie (Decision Coverage) verifică dacă fiecare ramură logică a fost parcursă.
Pentru fiecare condiție din cod a existat:
- un test care intră pe ramura „Da”
- un test care intră pe ramura „Nu”

Un exemplu important este verificarea: `if(totalXP < 0)`. S-a testat:
- un caz în care XP-ul final este negativ și metoda returnează 0
- un caz în care XP-ul rămâne pozitiv

### Acoperirea la nivel de condiție
Acoperirea la nivel de condiție (Condition Coverage) verifică fiecare condiție logică separat.
În această metodă condițiile sunt simple și conțin un singur predicat (o singură verificare), de exemplu: `upvotes <= 10`. Din acest motiv, odată obținută acoperirea la nivel de decizie, a fost acoperită automat și partea de condiții.

---

## Circuite independente și complexitate ciclomatică

A fost realizat și graful de control al metodei (CFG - Control Flow Graph), adică diagrama care arată toate traseele posibile prin cod.

```
graph TD
    Start((Start)) --> N1["1-3: totalXP=0, tier1Count, totalXP+="]

    N1 --> D2{"4: upvotes <= 10?"}
    D2 -- Da --> N12["17-19: totalPenalty=0, tier1Down, penalty+="]
    D2 -- Nu --> N3["5-6: tier2Count, totalXP+="]

    N3 --> D4{"7: upvotes <= 25?"}
    D4 -- Da --> N12
    D4 -- Nu --> N5["8-9: tier3Count, totalXP+="]

    N5 --> D6{"10: upvotes <= 50?"}
    D6 -- Da --> N12
    D6 -- Nu --> N7["11-12: tier4Count, totalXP+="]

    N7 --> D8{"13: upvotes <= 100?"}
    D8 -- Da --> N12
    D8 -- Nu --> N9["14-15: tier5Count, totalXP+="]
    N9 --> N12

    N12 --> D13{"20: downvotes <= 5?"}
    D13 -- Da --> N18["27: bonuses=0"]
    D13 -- Nu --> N14["21-22: tier2Down, penalty+="]

    N14 --> D15{"23: downvotes <= 15?"}
    D15 -- Da --> N18
    D15 -- Nu --> N16["24-25: tier3Down, penalty+="]
    N16 --> N18

    N18 --> D19{"28: upvotes >= 100?"}
    D19 -- Da --> N20["28: bonuses+=200"] --> D21{"29: upvotes >= 50?"}
    D19 -- Nu --> D21

    D21 -- Da --> N22["29: bonuses+=100"] --> D23{"30: upvotes >= 25?"}
    D21 -- Nu --> D23

    D23 -- Da --> N24["30: bonuses+=50"] --> D25{"31: upvotes >= 10?"}
    D23 -- Nu --> D25

    D25 -- Da --> N26["31: bonuses+=25"] --> N27["33: totalXP=up-pen+mil"]
    D25 -- Nu --> N27

    N27 --> D28{"34: totalXP < 0?"}
    D28 -- Da --> N29["34: return 0"]
    D28 -- Nu --> N30["34: return totalXP"]

    N29 --> Stop((Stop))
    N30 --> Stop
```

---

## Testarea bazată pe mutanți (Mutation Testing)

A fost rulat instrumentul Stryker.NET pentru a evalua calitatea testelor prin injectarea de mutanți (defecțiuni artificiale) în codul sursă al suitei de teste unitare din `GamificationTests.cs`.

### Analiza raportului Stryker (GamificationService)
* **Mutanți generați:** 147 
* **Mutanți omorâți (Killed):** 116
* **Mutanți supraviețuitori (Survived):** 31
* **Scor de mutație (Mutation Score):** ~79%

### Analiza mutanților echivalenți
În urma analizei, s-a observat că o parte din cei 31 de mutanți rămași sunt **echivalenți** (codul alterat dă același rezultat):
* **ID 4051 (Equality):** `if (upvotes <= 10)` a devenit `< 10`. Pentru `upvotes = 10`, codul original dă 100 XP. Mutantul sare linia, dar următoarea condiție adaugă `0` la XP și se oprește la pragul de 25, returnând tot 100 XP.
* **ID 4093 (Arithmetic):** Înmulțirea penalizării de tier 3 (`X * 1`) a fost schimbată în împărțire (`X / 1`). Deoarece constanta este `1`, rezultatul matematic este același.

### Teste suplimentare
Pentru a "omorî" 2 dintre mutanții neechivalenți rămași în viață, s-au adăugat teste specifice în `GamificationTests.cs`:

* **Mutant ID 4024 (Logical):** Condiția `(FirstName == "" && LastName == "")` a devenit `||`. S-a creat testul `Kill_Mutant_LogicalAnd...` cu un utilizator care are doar prenume. Testul verifică preluarea corectă a numelui și pică dacă e folosit `||`.
* **Mutant ID 4145 (Equality):** Verificarea de nivel `if (newLevel > oldLevel)` a devenit `>=`. S-a scris testul `Kill_Mutant_LevelUpCondition...` care acordă XP fără a crește nivelul. Mutantul ar acorda vouchere false, dar testul se asigură că lista de vouchere rămâne goală.

---

## Implementare de Referință (Console Test Runner)

Pe lângă testele xUnit integrate în proiect (`GamificationTests.cs`), a fost generat folosind AI-ul și un runner de teste standalone (consolă) care înglobează logica izolată pentru a testa foarte rapid implementarea formulei.

### Prompt Ai

> Am o metodă C# numită `CalculateXPFromVotes(int upvotes, int downvotes)` care calculează XP-ul unui utilizator pe baza voturilor. Upvotes oferă XP progresiv pe 5 niveluri descrescătoare (0-10, 11-25, 26-50, 51-100, >100), iar downvotes aplică penalizări pe 3 niveluri (0-5, 6-15, >15). De asemenea, există bonusuri cumulative la pragurile 10, 25, 50 și 100 upvotes. Rezultatul final este minim 0.
> Te rog să generezi mai întâi clasele de echivalență (valide și invalide) pentru intrările acestei metode și să identifici valorile de frontieră pentru fiecare clasă. Apoi, creează un fișier C# fără librării externe care să conțină logica metodei și un runner de teste care să acopere exhaustiv clasele de echivalență, valorile de frontieră, teste pentru frontiere simultane, și milestone-uri izolate.
> Fiecare test trebuie să afișeze PASS sau FAIL cu valorile de intrare și rezultatul obținut.

### Cod

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
        Section("Suplimentar: Verificare milestones individuale");
        Test("Milestone 10 – doar bonusul de 25 activ",   10,   0, 125);
        Test("Milestone 25 – bonusuri de 25+50 active",   25,   0, 295);
        Test("Milestone 50 – bonusuri de 25+50+100",      50,   0, 520);
        Test("Milestone 100 – toate bonusurile active",  100,   0, 870);
        Test("Sub orice milestone – 0 bonusuri",           9,   0,  90);

        // ── Teste Suplimentare: Frontiere simultane up+down ──────────
        Section("Suplimentar: Frontiere simultane upvotes + downvotes");
        Test("Ambele la prag: up=10, down=5",             10,   5, 115);
        Test("Ambele la prag: up=25, down=15",            25,  15, 255);
        Test("Ambele la prag: up=100, down=15",          100,  15, 830);
        Test("Extreme pozitiv: up=200, down=50",         200,  50, 995);

        // ── Teste Suplimentare: Un singur vot ────────────────────────
        Section("Suplimentar: Valori minime (un singur vot)");
        Test("Exact 1 upvote → 10 XP",                    1,   0,  10);
        Test("Exact 1 downvote → -2 XP → 0",              0,   1,   0);
        Test("1 upvote + 1 downvote → 10-2 = 8",          1,   1,   8);

        // ── Teste Suplimentare: Simetrie penalizare Tier 3 ───────────
        Section("Suplimentar: Penalizare Tier 3 verificata la valori mari");
        Test("50 downvotes cu 10 upvotes → 50 XP",       10,  50,  50);
        Test("100 downvotes cu 10 upvotes → 0 XP",       10, 100,   0);

        // ── Sumar ────────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine($"  Rezultat: {_passed} PASSED  |  {_failed} FAILED  |  {_passed + _failed} total");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        if (_failed > 0)
            Environment.Exit(1);
    }
}
```

### Rezultat

```text
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
