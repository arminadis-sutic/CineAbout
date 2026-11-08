using CineAboutApp.Models.Enums;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.General
{
    public static class CatalogSeeder
    {
        private sealed record CatalogEntry(
            string Name, int Year, int Duration, string Studio, string Language,
            string[] Genres, string Director, (string Actor, string Role)[] Cast, string Synopsis);

        private static readonly (string Name, string Country, int Founded)[] Studios =
        {
            ("CJ Entertainment", "Južna Koreja", 1995),
            ("Studio Ghibli", "Japan", 1985),
            ("UGC", "Francuska", 1971),
            ("Estudios Picasso", "Španija", 1991),
            ("Netflix", "SAD", 1997),
            ("Miramax", "SAD", 1979),
        };

        private static readonly string[] ExtraCountries = { "Japan", "Južna Koreja" };
        private static readonly string[] ExtraLanguages = { "Korejski", "Japanski" };

        private static readonly (string Name, PersonRoleType Role, int Year)[] People =
        {
            ("Christopher Nolan", PersonRoleType.Director, 1970), ("Quentin Tarantino", PersonRoleType.Director, 1963),
            ("Martin Scorsese", PersonRoleType.Director, 1942), ("Denis Villeneuve", PersonRoleType.Director, 1967),
            ("David Fincher", PersonRoleType.Director, 1962), ("James Cameron", PersonRoleType.Director, 1954),
            ("Francis Ford Coppola", PersonRoleType.Director, 1939), ("Ridley Scott", PersonRoleType.Director, 1937),
            ("Bong Joon-ho", PersonRoleType.Director, 1969), ("Damien Chazelle", PersonRoleType.Director, 1985),
            ("Todd Phillips", PersonRoleType.Director, 1970), ("Frank Darabont", PersonRoleType.Director, 1959),
            ("Robert Zemeckis", PersonRoleType.Director, 1952), ("Hayao Miyazaki", PersonRoleType.Director, 1941),
            ("Jean-Pierre Jeunet", PersonRoleType.Director, 1953), ("Guillermo del Toro", PersonRoleType.Director, 1964),
            ("Vince Gilligan", PersonRoleType.Director, 1967), ("Matt Duffer", PersonRoleType.Director, 1984),
            ("Peter Morgan", PersonRoleType.Director, 1963), ("David Benioff", PersonRoleType.Director, 1970),
            ("Johan Renck", PersonRoleType.Director, 1966), ("Baran bo Odar", PersonRoleType.Director, 1978),
            ("Lana Wachowski", PersonRoleType.Director, 1965),
            ("Leonardo DiCaprio", PersonRoleType.Actor, 1974), ("Brad Pitt", PersonRoleType.Actor, 1963),
            ("Margot Robbie", PersonRoleType.Actor, 1990), ("Robert De Niro", PersonRoleType.Actor, 1943),
            ("Tom Hanks", PersonRoleType.Actor, 1956), ("Joaquin Phoenix", PersonRoleType.Actor, 1974),
            ("Emma Stone", PersonRoleType.Actor, 1988), ("Ryan Gosling", PersonRoleType.Actor, 1980),
            ("Christian Bale", PersonRoleType.Actor, 1974), ("Timothée Chalamet", PersonRoleType.Actor, 1995),
            ("Zendaya", PersonRoleType.Actor, 1996), ("Joseph Gordon-Levitt", PersonRoleType.Actor, 1981),
            ("Elliot Page", PersonRoleType.Actor, 1987), ("Tom Hardy", PersonRoleType.Actor, 1977),
            ("Heath Ledger", PersonRoleType.Actor, 1979), ("Aaron Eckhart", PersonRoleType.Actor, 1968),
            ("Michael Caine", PersonRoleType.Actor, 1933), ("John Travolta", PersonRoleType.Actor, 1954),
            ("Samuel L. Jackson", PersonRoleType.Actor, 1948), ("Uma Thurman", PersonRoleType.Actor, 1970),
            ("Bruce Willis", PersonRoleType.Actor, 1955), ("Kate Winslet", PersonRoleType.Actor, 1975),
            ("Billy Zane", PersonRoleType.Actor, 1966), ("Sam Worthington", PersonRoleType.Actor, 1976),
            ("Zoe Saldaña", PersonRoleType.Actor, 1978), ("Sigourney Weaver", PersonRoleType.Actor, 1949),
            ("Marlon Brando", PersonRoleType.Actor, 1924), ("Al Pacino", PersonRoleType.Actor, 1940),
            ("James Caan", PersonRoleType.Actor, 1940), ("Robert Duvall", PersonRoleType.Actor, 1931),
            ("Matthew McConaughey", PersonRoleType.Actor, 1969), ("Anne Hathaway", PersonRoleType.Actor, 1982),
            ("Jessica Chastain", PersonRoleType.Actor, 1977), ("Edward Norton", PersonRoleType.Actor, 1969),
            ("Helena Bonham Carter", PersonRoleType.Actor, 1966), ("Keanu Reeves", PersonRoleType.Actor, 1964),
            ("Laurence Fishburne", PersonRoleType.Actor, 1961), ("Carrie-Anne Moss", PersonRoleType.Actor, 1967),
            ("Hugo Weaving", PersonRoleType.Actor, 1960), ("Russell Crowe", PersonRoleType.Actor, 1964),
            ("Connie Nielsen", PersonRoleType.Actor, 1965), ("Song Kang-ho", PersonRoleType.Actor, 1967),
            ("Choi Woo-shik", PersonRoleType.Actor, 1990), ("Park So-dam", PersonRoleType.Actor, 1991),
            ("Lee Sun-kyun", PersonRoleType.Actor, 1975), ("John Legend", PersonRoleType.Actor, 1978),
            ("Rebecca Ferguson", PersonRoleType.Actor, 1983), ("Oscar Isaac", PersonRoleType.Actor, 1979),
            ("Zazie Beetz", PersonRoleType.Actor, 1991), ("Jonah Hill", PersonRoleType.Actor, 1983),
            ("Jamie Foxx", PersonRoleType.Actor, 1967), ("Christoph Waltz", PersonRoleType.Actor, 1956),
            ("Ray Liotta", PersonRoleType.Actor, 1954), ("Joe Pesci", PersonRoleType.Actor, 1943),
            ("Lorraine Bracco", PersonRoleType.Actor, 1954), ("Tim Robbins", PersonRoleType.Actor, 1958),
            ("Morgan Freeman", PersonRoleType.Actor, 1937), ("Bob Gunton", PersonRoleType.Actor, 1945),
            ("Robin Wright", PersonRoleType.Actor, 1966), ("Gary Sinise", PersonRoleType.Actor, 1955),
            ("Sally Field", PersonRoleType.Actor, 1946), ("Rumi Hiiragi", PersonRoleType.Actor, 1987),
            ("Miyu Irino", PersonRoleType.Actor, 1988), ("Mari Natsuki", PersonRoleType.Actor, 1952),
            ("Audrey Tautou", PersonRoleType.Actor, 1976), ("Mathieu Kassovitz", PersonRoleType.Actor, 1967),
            ("Ivana Baquero", PersonRoleType.Actor, 1994), ("Sergi López", PersonRoleType.Actor, 1965),
            ("Maribel Verdú", PersonRoleType.Actor, 1970), ("Doug Jones", PersonRoleType.Actor, 1960),
            ("Bryan Cranston", PersonRoleType.Actor, 1956), ("Aaron Paul", PersonRoleType.Actor, 1979),
            ("Anna Gunn", PersonRoleType.Actor, 1968), ("Dean Norris", PersonRoleType.Actor, 1963),
            ("Millie Bobby Brown", PersonRoleType.Actor, 2004), ("Finn Wolfhard", PersonRoleType.Actor, 2002),
            ("Winona Ryder", PersonRoleType.Actor, 1971), ("David Harbour", PersonRoleType.Actor, 1975),
            ("Claire Foy", PersonRoleType.Actor, 1984), ("Olivia Colman", PersonRoleType.Actor, 1974),
            ("Matt Smith", PersonRoleType.Actor, 1982), ("Imelda Staunton", PersonRoleType.Actor, 1956),
            ("Emilia Clarke", PersonRoleType.Actor, 1986), ("Kit Harington", PersonRoleType.Actor, 1986),
            ("Peter Dinklage", PersonRoleType.Actor, 1969), ("Lena Headey", PersonRoleType.Actor, 1973),
            ("Jared Harris", PersonRoleType.Actor, 1961), ("Stellan Skarsgård", PersonRoleType.Actor, 1951),
            ("Emily Watson", PersonRoleType.Actor, 1967), ("Louis Hofmann", PersonRoleType.Actor, 1997),
            ("Lisa Vicari", PersonRoleType.Actor, 1997), ("Oliver Masucci", PersonRoleType.Actor, 1968),
        };

        private static readonly CatalogEntry[] Catalog =
        {
            new CatalogEntry("Inception", 2010, 148, "Warner Bros.", "English", new[] { "Akcija", "Sci-Fi", "Triler" }, "Christopher Nolan",
                new[] { ("Leonardo DiCaprio", "Dom Cobb"), ("Joseph Gordon-Levitt", "Arthur"), ("Elliot Page", "Ariadne"), ("Tom Hardy", "Eames") },
                "A skilled thief who steals secrets from people's dreams is offered a chance to erase his past: plant an idea inside a target's mind instead of stealing one."),
            new CatalogEntry("The Dark Knight", 2008, 152, "Warner Bros.", "English", new[] { "Akcija", "Krimi", "Drama" }, "Christopher Nolan",
                new[] { ("Christian Bale", "Bruce Wayne / Batman"), ("Heath Ledger", "Joker"), ("Aaron Eckhart", "Harvey Dent"), ("Michael Caine", "Alfred Pennyworth") },
                "Batman faces the Joker, an anarchic criminal mastermind who pushes Gotham City to its limits and forces its heroes to question what they stand for."),
            new CatalogEntry("Pulp Fiction", 1994, 154, "Miramax", "English", new[] { "Krimi", "Drama" }, "Quentin Tarantino",
                new[] { ("John Travolta", "Vincent Vega"), ("Samuel L. Jackson", "Jules Winnfield"), ("Uma Thurman", "Mia Wallace"), ("Bruce Willis", "Butch Coolidge") },
                "The lives of two hitmen, a boxer, a gangster and his wife intertwine in four tales of violence and redemption in Los Angeles."),
            new CatalogEntry("Titanic", 1997, 195, "Paramount", "English", new[] { "Drama", "Romantika" }, "James Cameron",
                new[] { ("Leonardo DiCaprio", "Jack Dawson"), ("Kate Winslet", "Rose DeWitt Bukater"), ("Billy Zane", "Cal Hockley") },
                "A young artist and an upper-class woman fall in love aboard the ill-fated maiden voyage of the RMS Titanic."),
            new CatalogEntry("Avatar", 2009, 162, "20th Century", "English", new[] { "Sci-Fi", "Avantura", "Akcija" }, "James Cameron",
                new[] { ("Sam Worthington", "Jake Sully"), ("Zoe Saldaña", "Neytiri"), ("Sigourney Weaver", "Dr. Grace Augustine") },
                "A paralysed marine is sent to the moon Pandora, where he becomes torn between following orders and protecting the world he has come to love."),
            new CatalogEntry("The Godfather", 1972, 175, "Paramount", "English", new[] { "Krimi", "Drama" }, "Francis Ford Coppola",
                new[] { ("Marlon Brando", "Vito Corleone"), ("Al Pacino", "Michael Corleone"), ("James Caan", "Sonny Corleone"), ("Robert Duvall", "Tom Hagen") },
                "The aging patriarch of an organised crime dynasty transfers control of his empire to his reluctant youngest son."),
            new CatalogEntry("Interstellar", 2014, 169, "Paramount", "English", new[] { "Sci-Fi", "Drama", "Avantura" }, "Christopher Nolan",
                new[] { ("Matthew McConaughey", "Cooper"), ("Anne Hathaway", "Brand"), ("Jessica Chastain", "Murph"), ("Michael Caine", "Professor Brand") },
                "With Earth dying, a team of explorers travels through a wormhole in search of a new home for humanity."),
            new CatalogEntry("Fight Club", 1999, 139, "20th Century", "English", new[] { "Drama", "Triler" }, "David Fincher",
                new[] { ("Brad Pitt", "Tyler Durden"), ("Edward Norton", "The Narrator"), ("Helena Bonham Carter", "Marla Singer") },
                "An insomniac office worker and a charismatic soap maker form an underground fight club that evolves into something far more dangerous."),
            new CatalogEntry("The Matrix", 1999, 136, "Warner Bros.", "English", new[] { "Akcija", "Sci-Fi" }, "Lana Wachowski",
                new[] { ("Keanu Reeves", "Neo"), ("Laurence Fishburne", "Morpheus"), ("Carrie-Anne Moss", "Trinity"), ("Hugo Weaving", "Agent Smith") },
                "A hacker discovers that reality is a simulation controlled by machines and joins a rebellion to free humankind."),
            new CatalogEntry("Gladiator", 2000, 155, "Universal", "English", new[] { "Akcija", "Drama", "Avantura" }, "Ridley Scott",
                new[] { ("Russell Crowe", "Maximus"), ("Joaquin Phoenix", "Commodus"), ("Connie Nielsen", "Lucilla") },
                "A betrayed Roman general is forced into slavery and becomes a gladiator, seeking revenge against the corrupt emperor who destroyed his family."),
            new CatalogEntry("Parasite", 2019, 132, "CJ Entertainment", "Korejski", new[] { "Triler", "Drama", "Komedija" }, "Bong Joon-ho",
                new[] { ("Song Kang-ho", "Kim Ki-taek"), ("Choi Woo-shik", "Kim Ki-woo"), ("Park So-dam", "Kim Ki-jung"), ("Lee Sun-kyun", "Park Dong-ik") },
                "A poor family schemes to become employed by a wealthy household, with consequences nobody saw coming."),
            new CatalogEntry("La La Land", 2016, 128, "Lionsgate", "English", new[] { "Romantika", "Komedija", "Drama" }, "Damien Chazelle",
                new[] { ("Ryan Gosling", "Sebastian Wilder"), ("Emma Stone", "Mia Dolan"), ("John Legend", "Keith") },
                "A jazz pianist and an aspiring actress fall in love while chasing their dreams in Los Angeles."),
            new CatalogEntry("Dune", 2021, 155, "Warner Bros.", "English", new[] { "Sci-Fi", "Avantura", "Drama" }, "Denis Villeneuve",
                new[] { ("Timothée Chalamet", "Paul Atreides"), ("Zendaya", "Chani"), ("Rebecca Ferguson", "Lady Jessica"), ("Oscar Isaac", "Duke Leto Atreides") },
                "A gifted young man travels to the dangerous desert planet Arrakis to ensure the future of his family and people."),
            new CatalogEntry("Joker", 2019, 122, "Warner Bros.", "English", new[] { "Krimi", "Triler", "Drama" }, "Todd Phillips",
                new[] { ("Joaquin Phoenix", "Arthur Fleck"), ("Robert De Niro", "Murray Franklin"), ("Zazie Beetz", "Sophie Dumond") },
                "A mentally troubled comedian, ignored and mistreated by society, descends into madness and becomes the infamous Joker."),
            new CatalogEntry("The Wolf of Wall Street", 2013, 180, "Paramount", "English", new[] { "Komedija", "Krimi", "Drama" }, "Martin Scorsese",
                new[] { ("Leonardo DiCaprio", "Jordan Belfort"), ("Jonah Hill", "Donnie Azoff"), ("Margot Robbie", "Naomi Lapaglia"), ("Matthew McConaughey", "Mark Hanna") },
                "The rise and fall of New York stockbroker Jordan Belfort, whose firm built a fortune on corruption and excess."),
            new CatalogEntry("Django Unchained", 2012, 165, "Sony", "English", new[] { "Drama", "Akcija", "Avantura" }, "Quentin Tarantino",
                new[] { ("Jamie Foxx", "Django"), ("Christoph Waltz", "Dr. King Schultz"), ("Leonardo DiCaprio", "Calvin Candie"), ("Samuel L. Jackson", "Stephen") },
                "With a bounty hunter's help, a freed slave sets out to rescue his wife from a brutal plantation owner in the American South."),
            new CatalogEntry("Once Upon a Time in Hollywood", 2019, 161, "Sony", "English", new[] { "Komedija", "Drama" }, "Quentin Tarantino",
                new[] { ("Leonardo DiCaprio", "Rick Dalton"), ("Brad Pitt", "Cliff Booth"), ("Margot Robbie", "Sharon Tate") },
                "A fading television actor and his stunt double navigate the changing film industry of 1969 Los Angeles."),
            new CatalogEntry("Goodfellas", 1990, 145, "Warner Bros.", "English", new[] { "Krimi", "Drama" }, "Martin Scorsese",
                new[] { ("Ray Liotta", "Henry Hill"), ("Robert De Niro", "James Conway"), ("Joe Pesci", "Tommy DeVito"), ("Lorraine Bracco", "Karen Hill") },
                "The story of Henry Hill and his life in the mob, covering his relationships with his wife and his partners in crime."),
            new CatalogEntry("The Shawshank Redemption", 1994, 142, "Sony", "English", new[] { "Drama", "Krimi" }, "Frank Darabont",
                new[] { ("Tim Robbins", "Andy Dufresne"), ("Morgan Freeman", "Ellis 'Red' Redding"), ("Bob Gunton", "Warden Norton") },
                "Two imprisoned men bond over many years, finding solace and eventual redemption through acts of common decency."),
            new CatalogEntry("Forrest Gump", 1994, 142, "Paramount", "English", new[] { "Drama", "Romantika", "Komedija" }, "Robert Zemeckis",
                new[] { ("Tom Hanks", "Forrest Gump"), ("Robin Wright", "Jenny Curran"), ("Gary Sinise", "Lt. Dan Taylor"), ("Sally Field", "Mrs. Gump") },
                "The life of a kind man with a low IQ who unwittingly becomes part of defining moments of American history."),
            new CatalogEntry("Spirited Away", 2001, 125, "Studio Ghibli", "Japanski", new[] { "Animacija", "Fantazija", "Avantura" }, "Hayao Miyazaki",
                new[] { ("Rumi Hiiragi", "Chihiro (voice)"), ("Miyu Irino", "Haku (voice)"), ("Mari Natsuki", "Yubaba (voice)") },
                "A ten-year-old girl wanders into a world ruled by gods, witches and spirits, where humans are changed into beasts."),
            new CatalogEntry("Amélie", 2001, 122, "UGC", "Francuski", new[] { "Komedija", "Romantika" }, "Jean-Pierre Jeunet",
                new[] { ("Audrey Tautou", "Amélie Poulain"), ("Mathieu Kassovitz", "Nino Quincampoix") },
                "A whimsical Parisian waitress decides to secretly improve the lives of those around her, until she must find happiness of her own."),
            new CatalogEntry("Pan's Labyrinth", 2006, 118, "Estudios Picasso", "Španski", new[] { "Fantazija", "Drama", "Horor" }, "Guillermo del Toro",
                new[] { ("Ivana Baquero", "Ofelia"), ("Sergi López", "Captain Vidal"), ("Maribel Verdú", "Mercedes"), ("Doug Jones", "The Faun") },
                "In 1944 Spain, a girl escapes the cruelty of her stepfather by entering an eerie but captivating fantasy world."),
            new CatalogEntry("Breaking Bad", 2008, 47, "Sony", "English", new[] { "Krimi", "Drama", "Triler" }, "Vince Gilligan",
                new[] { ("Bryan Cranston", "Walter White"), ("Aaron Paul", "Jesse Pinkman"), ("Anna Gunn", "Skyler White"), ("Dean Norris", "Hank Schrader") },
                "A high school chemistry teacher diagnosed with cancer turns to manufacturing methamphetamine to secure his family's future."),
            new CatalogEntry("Stranger Things", 2016, 50, "Netflix", "English", new[] { "Sci-Fi", "Horor", "Misterija" }, "Matt Duffer",
                new[] { ("Millie Bobby Brown", "Eleven"), ("Finn Wolfhard", "Mike Wheeler"), ("Winona Ryder", "Joyce Byers"), ("David Harbour", "Jim Hopper") },
                "When a boy vanishes, a small town uncovers secret experiments, supernatural forces and one strange little girl."),
            new CatalogEntry("The Crown", 2016, 58, "Sony", "English", new[] { "Drama" }, "Peter Morgan",
                new[] { ("Claire Foy", "Queen Elizabeth II"), ("Olivia Colman", "Queen Elizabeth II"), ("Matt Smith", "Prince Philip"), ("Imelda Staunton", "Queen Elizabeth II") },
                "The political rivalries and romances of Queen Elizabeth II's reign and the events that shaped the second half of the twentieth century."),
            new CatalogEntry("Game of Thrones", 2011, 57, "Warner Bros.", "English", new[] { "Fantazija", "Drama", "Avantura" }, "David Benioff",
                new[] { ("Emilia Clarke", "Daenerys Targaryen"), ("Kit Harington", "Jon Snow"), ("Peter Dinklage", "Tyrion Lannister"), ("Lena Headey", "Cersei Lannister") },
                "Nine noble families fight for control of the lands of Westeros while an ancient enemy returns after being dormant for millennia."),
            new CatalogEntry("Chernobyl", 2019, 60, "Warner Bros.", "English", new[] { "Drama", "Triler" }, "Johan Renck",
                new[] { ("Jared Harris", "Valery Legasov"), ("Stellan Skarsgård", "Boris Shcherbina"), ("Emily Watson", "Ulana Khomyuk") },
                "In April 1986 an explosion at the Chernobyl nuclear power plant becomes one of the worst man-made catastrophes in history."),
            new CatalogEntry("Dark", 2017, 53, "Netflix", "Njemački", new[] { "Sci-Fi", "Misterija", "Triler" }, "Baran bo Odar",
                new[] { ("Louis Hofmann", "Jonas Kahnwald"), ("Lisa Vicari", "Martha Nielsen"), ("Oliver Masucci", "Ulrich Nielsen") },
                "A missing child sets four families on a frantic hunt for answers as they unearth a mind-bending time-travel conspiracy."),
        };

        public static async Task ApplyAsync(ApplicationDbContext db, CancellationToken ct = default)
        {
            var countries = await db.Countries.OrderBy(c => c.Id).ToFirstWinsDictionaryAsync(c => c.Name, ct);
            foreach (var name in ExtraCountries.Where(n => !countries.ContainsKey(n)))
                db.Countries.Add(new Country { Name = name });

            var languages = await db.Languages.OrderBy(l => l.Id).ToFirstWinsDictionaryAsync(l => l.Name, ct);
            foreach (var name in ExtraLanguages.Where(n => !languages.ContainsKey(n)))
                db.Languages.Add(new Language { Name = name });
            await db.SaveChangesAsync(ct);

            countries = await db.Countries.OrderBy(c => c.Id).ToFirstWinsDictionaryAsync(c => c.Name, ct);
            languages = await db.Languages.OrderBy(l => l.Id).ToFirstWinsDictionaryAsync(l => l.Name, ct);

            var studios = await db.Studios.OrderBy(s => s.Id).ToFirstWinsDictionaryAsync(s => s.Name, ct);
            foreach (var s in Studios.Where(s => !studios.ContainsKey(s.Name) && countries.ContainsKey(s.Country)))
            {
                db.Studios.Add(new Studio
                {
                    Name = s.Name,
                    CountryId = countries[s.Country].Id,
                    FoundedYear = s.Founded,
                    Description = $"{s.Name} je poznati filmski studio."
                });
            }
            await db.SaveChangesAsync(ct);
            studios = await db.Studios.OrderBy(s => s.Id).ToFirstWinsDictionaryAsync(s => s.Name, ct);

            var persons = (await db.Persons.ToListAsync(ct))
                .GroupBy(p => p.FullName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var p in People.Where(p => !persons.ContainsKey(p.Name)))
            {
                db.Persons.Add(new Person
                {
                    FullName = p.Name,
                    RoleType = p.Role,
                    BirthDate = new DateTime(p.Year, 1, 1),
                    Biography = $"{p.Name} je poznati filmski {(p.Role == PersonRoleType.Actor ? "glumac" : "režiser")}.",
                    PhotoUrl = string.Empty
                });
            }
            await db.SaveChangesAsync(ct);
            persons = (await db.Persons.ToListAsync(ct))
                .GroupBy(p => p.FullName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var genres = await db.Genres.OrderBy(g => g.Id).ToFirstWinsDictionaryAsync(g => g.Name, ct);
            var movies = await db.Movies.ToListAsync(ct);
            foreach (var entry in Catalog)
            {
                var movie = movies.FirstOrDefault(m => m.Title == entry.Name);
                if (movie == null || !IsGeneratedSynopsis(movie)) continue;
                if (!languages.TryGetValue(entry.Language, out var language) || !studios.TryGetValue(entry.Studio, out var studio))
                    continue;

                movie.Synopsis = entry.Synopsis;
                movie.ReleaseYear = entry.Year;
                movie.Duration = entry.Duration;
                movie.LanguageId = language.Id;
                movie.StudioId = studio.Id;
                movie.PosterUrl = CatalogImages.Posters.TryGetValue(entry.Name, out var poster)
                    ? CatalogImages.BaseUrl + poster
                    : string.Empty;

                db.MovieGenres.RemoveRange(await db.MovieGenres.Where(x => x.MovieId == movie.Id).ToListAsync(ct));
                db.MovieCasts.RemoveRange(await db.MovieCasts.Where(x => x.MovieId == movie.Id).ToListAsync(ct));

                foreach (var g in entry.Genres.Where(genres.ContainsKey))
                    db.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = genres[g].Id });

                if (persons.TryGetValue(entry.Director, out var director))
                    db.MovieCasts.Add(new MovieCast { MovieId = movie.Id, PersonId = director.Id, RoleInMovie = "Director" });
                foreach (var (actor, role) in entry.Cast.Where(c => persons.ContainsKey(c.Actor)))
                    db.MovieCasts.Add(new MovieCast { MovieId = movie.Id, PersonId = persons[actor].Id, RoleInMovie = role });
            }
            await db.SaveChangesAsync(ct);

            foreach (var person in persons.Values.Where(p => p.PhotoUrl.Length == 0 || IsGeneratedPlaceholder(p)))
            {
                var wasPlaceholder = person.PhotoUrl.Length > 0;
                if (!CatalogImages.People.TryGetValue(person.FullName, out var image))
                {
                    if (wasPlaceholder) person.PhotoUrl = string.Empty;
                    continue;
                }

                person.PhotoUrl = CatalogImages.BaseUrl + image.Photo;
                person.BirthDate = image.BirthDate;
            }

            var profiles = await db.UserProfiles
                .Where(p => p.ProfileImageUrl != null && p.ProfileImageUrl.StartsWith("/media/profiles/")
                    && db.MyAppUsers.Any(u => u.Id == p.UserId && u.Email.EndsWith("@cineabout.test")))
                .ToListAsync(ct);
            foreach (var profile in profiles) profile.ProfileImageUrl = null;

            await db.SaveChangesAsync(ct);
        }

        private static bool IsGeneratedPlaceholder(Person person) =>
            string.Equals(person.PhotoUrl, $"/media/persons/{DataSeeder.Slug(person.FullName)}.png", StringComparison.Ordinal)
            || string.Equals(person.PhotoUrl, $"/media/persons/{DataSeeder.Slug(person.FullName)}.jpg", StringComparison.Ordinal);

        private static bool IsGeneratedSynopsis(Movie movie) =>
            string.IsNullOrWhiteSpace(movie.Synopsis)
            || movie.Synopsis.StartsWith($"{movie.Title} je filmska priča", StringComparison.Ordinal);
    }
}
