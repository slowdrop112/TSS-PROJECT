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

```text
“Creează graful de control (Control Flow Graph) în limbaj Mermaid pentru logica completă din CalculateXPFromVotes. Include calculul pe tier-uri pentru upvotes și downvotes, bonusurile și limitarea finală la 0”
```
