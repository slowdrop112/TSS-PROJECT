# TSS-PROJECT

# Testare unitară în C# – GamificationService

## Descriere

A fost testată componenta de gamificare din aplicația **Uniflow**, responsabilă pentru acordarea punctelor XP utilizatorilor în funcție de activitatea lor.

Clasa testată este:

```csharp
GamificationService
```

Metoda analizată:

```csharp
CalculateXPFromVotes(int upvotes, int downvotes)
```

Scopul testării a fost verificarea corectitudinii calculului XP-ului și validarea comportamentului aplicației în diferite scenarii, inclusiv cazuri limită.

---

# Funcționalitatea metodei

Metoda `CalculateXPFromVotes` calculează numărul total de puncte XP obținute de un utilizator pentru o notiță, în funcție de:

- numărul de voturi pozitive (`upvotes`)
- numărul de voturi negative (`downvotes`)

---

# Reguli de calcul XP

## XP pentru upvotes

XP-ul este acordat progresiv pe niveluri:

| Interval upvotes | XP acordat |
|---|---|
| 0 - 10 | +10 XP / vot |
| 11 - 25 | +8 XP / vot |
| 26 - 50 | +5 XP / vot |
| 51 - 100 | +3 XP / vot |
| peste 100 | +2 XP / vot |

---

## Penalizări pentru downvotes

| Interval downvotes | Penalizare |
|---|---|
| 0 - 5 | -2 XP / vot |
| 6 - 15 | -3 XP / vot |
| peste 15 | -1 XP / vot |

---

## Bonusuri de popularitate

La atingerea anumitor praguri de apreciere se acordă bonusuri suplimentare:

| Prag upvotes | Bonus |
|---|---|
| 10 | +25 XP |
| 25 | +50 XP |
| 50 | +100 XP |
| 100 | +200 XP |

---

## Regula finală

Dacă XP-ul total devine negativ:

```csharp
if(totalXP < 0)
```

metoda returnează:

```csharp
0
```

---

# Etapele metodei

Metoda este împărțită în mai multe etape:

1. Calcularea XP-ului pentru upvotes
2. Aplicarea penalizărilor pentru downvotes
3. Aplicarea bonusurilor pentru praguri
4. Verificarea rezultatului final

---

# Scopul sistemului

Sistemul urmărește:

- recompensarea conținutului apreciat
- penalizarea notițelor slab evaluate
- încurajarea contribuțiilor de calitate

---

# Partiționarea în clase de echivalență

Pentru testare, valorile de intrare au fost împărțite în clase de echivalență.

## Upvotes

- valori invalide
- intervalul 0-10
- intervalul 11-25
- intervalul 26-50
- intervalul 51-100
- valori peste 100

## Downvotes

- valori invalide
- intervalul 0-5
- intervalul 6-15
- valori peste 15

Pentru fiecare categorie au fost selectate valori reprezentative pentru verificarea corectitudinii rezultatului.

---

# Analiza valorilor de frontieră

Au fost testate valorile aflate exact la limitele intervalelor pentru identificarea eventualelor erori de tip *off-by-one*.

## Exemple testate

### Upvotes

```text
9, 10, 11
24, 25, 26
49, 50, 51
99, 100, 101
```

### Downvotes

```text
4, 5, 6
14, 15, 16
```

Rezultatele au confirmat aplicarea corectă a regulilor la trecerea dintre praguri.

---

# Acoperirea la nivel de instrucțiune

Statement Coverage verifică dacă fiecare instrucțiune importantă din metodă a fost executată cel puțin o dată.

## Scenarii utilizate

### Top Contributor

```text
upvotes = 150
downvotes = 0
```

Acest test:

- parcurge toate nivelurile de upvotes
- activează toate bonusurile

---

### Spammer

```text
upvotes = 0
downvotes = 100
```

Acest test verifică:

- penalizările mari
- plafonarea XP-ului la 0

Prin aceste scenarii au fost executate toate instrucțiunile importante ale metodei.

---

# Acoperirea la nivel de decizie

Decision Coverage verifică dacă fiecare ramură logică a fost parcursă.

Pentru fiecare condiție din cod au fost create:

- un test pentru ramura „True”
- un test pentru ramura „False”

## Exemplu

```csharp
if(totalXP < 0)
```

Au fost testate:

- un caz în care XP-ul final devine negativ
- un caz în care XP-ul rămâne pozitiv

---

# Acoperirea la nivel de condiție

Condition Coverage verifică fiecare condiție logică separat.

În această metodă condițiile sunt simple și conțin un singur predicat, de exemplu:

```csharp
upvotes <= 10
```

Din acest motiv, obținerea Decision Coverage a acoperit automat și partea de Condition Coverage.

---

# Graful de control și complexitatea ciclomatică

A fost realizat și graful de control al metodei:

```text
CFG - Control Flow Graph
```

Pe baza acestuia a fost calculată complexitatea ciclomatică folosind formula lui McCabe:

```text
C = P + 1
```

unde:

- `C` = complexitatea ciclomatică
- `P` = numărul de decizii din cod

## Rezultat

```text
C = 11 + 1 = 12
```

Metoda are:

```text
12 circuite independente
```

---

# Exemple de trasee independente

- caz simplu cu puține upvotes și puține downvotes
- caz cu penalizare mare din cauza downvotes
- caz cu multe upvotes și toate bonusurile activate

---

# Concluzie

Testarea unitară a confirmat că metoda:

- calculează corect XP-ul
- aplică bonusurile și penalizările conform regulilor
- tratează corect cazurile limită
- plafonează corect valorile negative la 0

Prin utilizarea:

- claselor de echivalență
- analizei valorilor de frontieră
- statement coverage
- decision coverage
- condition coverage
- complexității ciclomatice

s-a obținut o verificare completă și riguroasă a funcționalității metodei.
