# Discover recommender

Implementation: `CineAboutApp.Services/Controller/RecommendationService.cs`
Endpoints: `GET /api/Discover?count=10` and `POST /api/Discover/swipe`

The Discover screen shows one card at a time. The recommender is a **hybrid, explainable** model that combines
content-based (genre) scoring, collaborative filtering (similar users) and a popularity prior.

## Candidates

Visible movies/series that the user has **not** already reviewed, swiped (either direction) or put on a list.

## Signals (all are written by the real application)

| Signal | Written by | Used for |
|---|---|---|
| Preferred genres | `PUT /api/UserProfile/me/genres` (`UserGenrePreferences`) | genre affinity +1.0 per genre |
| Ratings | `POST /api/Review` (`Reviews.Rating`) | genre affinity `(rating - 5) / 5` per genre, and user similarity |
| Swipes | `POST /api/Discover/swipe` (`SwipeHistories`) | genre affinity +0.5 (right) / -0.5 (left) |
| Watchlist / Interested | `PUT /api/Watchlist/{movieId}` (`WatchlistItems`) | genre affinity +0.3 per genre |
| Community ratings | all users' reviews | similar-users score and popularity |

Removed reviews are ignored everywhere.

## Scoring

For every candidate `m`:

```
score(m) = 0.5 * genre(m) + 0.3 * similarUsers(m) + 0.2 * popularity(m)
```

* **genre(m)** (0..1) - per-genre affinities are summed from the signals above and divided by the largest
  absolute affinity (range -1..1). `genre(m)` is the mean affinity of the movie's genres mapped to 0..1.
  A movie without genres, or a user without genre signals, gets the neutral value 0.5.
* **similarUsers(m)** (0..1) - for every other user with at least 2 movies rated in common,
  `similarity = 1 - mean(|myRating - theirRating|) / 10`. Users with `similarity >= 0.6` are kept (top 20).
  `similarUsers(m) = sum(similarity * rating / 10) / sum(similarity)` over those users who rated `m`.
* **popularity(m)** (0..1) - Bayesian average of the community rating,
  `(n * avg + 3 * globalMean) / (n + 3) / 10`, so titles with few reviews are pulled to the global mean.
  Titles without reviews get `globalMean / 10`.

Special cases (documented because the implementation follows them exactly):

* If **no user in the community is similar** to the current user for movie `m` (no overlap), the similar-users
  term is dropped and the remaining weights are renormalised: `score = (0.5 * genre + 0.2 * popularity) / 0.7`.
* **Cold start** (no genre signal and no similar users) - ranking uses `popularity` only.

The top `count` candidates (max 20) by score are returned.

## Explanations

Each card carries a human readable `reason` plus the individual component scores
(`genreScore`, `similarUsersScore`, `popularityScore`). The reason is chosen in this order:

1. `similarUsers >= 0.6` - "Sviđa se N korisnika sličnog ukusa (njihova prosječna ocjena: X)."
2. `genre >= 0.6` - the genre of the candidate with the highest affinity is named, together with the best
   anchor: "Jer ste film 'Inception' ocijenili sa 9/10 (zajednički žanr: Akcija)." If the user has no highly rated
   movie in that genre: "Odgovara vašem omiljenom žanru: ..." (profile preference) or
   "Slično naslovima koji vas zanimaju (žanr: ...)".
3. Otherwise - "Popularno u zajednici (prosječna ocjena X, recenzija: N)."

## Swiping

`POST /api/Discover/swipe { movieId, direction }` stores the swipe in `SwipeHistories` (one row per user and
movie). A **right** swipe also adds the title to the user's **Interested** list; a **left** swipe removes it from
all future Discover results. Both actions feed back into the scoring above.
