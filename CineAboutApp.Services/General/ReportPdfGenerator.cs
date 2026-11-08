using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text;

namespace CineAboutApp.Services.General;

public class ReportPdfGenerator(HtmlToPdfGenerator pdfGenerator, ILogger<ReportPdfGenerator> logger)
{
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    public async Task<byte[]> GenerateCommunityActivityReportPdfAsync(
        DateTime startDate,
        DateTime endDate,
        List<(DateTime Date, int NewRegistrations, int NewReviews, int NewThreads, int QuizSessions)> dailyStats,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating Community Activity Report PDF for {Start} - {End}", startDate, endDate);

        var days = Math.Max(1, dailyStats.Count);
        var totalRegistrations = dailyStats.Sum(d => d.NewRegistrations);
        var totalReviews = dailyStats.Sum(d => d.NewReviews);
        var totalThreads = dailyStats.Sum(d => d.NewThreads);
        var totalQuiz = dailyStats.Sum(d => d.QuizSessions);
        var grandTotal = totalRegistrations + totalReviews + totalThreads + totalQuiz;

        var weekly = dailyStats
            .GroupBy(d => d.Date.Date.AddDays(-(((int)d.Date.DayOfWeek + 6) % 7)))
            .OrderBy(g => g.Key)
            .Select(g => (
                Start: g.Key,
                Days: g.Count(),
                Registrations: g.Sum(x => x.NewRegistrations),
                Reviews: g.Sum(x => x.NewReviews),
                Threads: g.Sum(x => x.NewThreads),
                Quiz: g.Sum(x => x.QuizSessions)))
            .ToList();

        var html = new StringBuilder();
        html.AppendLine("<div class='report-container'>");
        html.AppendLine("<h1 class='report-title'>IZVJEŠTAJ AKTIVNOSTI ZAJEDNICE</h1>");
        html.AppendLine($"<p class='report-subtitle'>Period: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy} ({dailyStats.Count} dana)</p>");
        html.AppendLine($"<p class='report-date'>Generirano: {DateTime.UtcNow:dd.MM.yyyy HH:mm}</p>");

        html.AppendLine("<h2>Ukupno u periodu</h2>");
        html.AppendLine("<table class='stats-table'>");
        html.AppendLine($"<tr><td>Ukupno novih registracija</td><td class='stat-value'>{totalRegistrations}</td></tr>");
        html.AppendLine($"<tr><td>Ukupno novih recenzija</td><td class='stat-value'>{totalReviews}</td></tr>");
        html.AppendLine($"<tr><td>Ukupno novih threadova</td><td class='stat-value'>{totalThreads}</td></tr>");
        html.AppendLine($"<tr><td>Ukupno odigranih kviz sesija</td><td class='stat-value'>{totalQuiz}</td></tr>");
        html.AppendLine($"<tr><td>Prosječna dnevna aktivnost (sve vrste, po danu)</td><td class='stat-value'>{(grandTotal / (double)days).ToString("0.00", CultureInfo.InvariantCulture)}</td></tr>");
        html.AppendLine("</table>");

        html.AppendLine("<h2>Aktivnost po danima</h2>");
        html.AppendLine(BarChartSvg(
            dailyStats.Select(d => d.Date.ToString("dd.MM.", CultureInfo.InvariantCulture)).ToList(),
            dailyStats.Select(d => d.NewRegistrations + d.NewReviews + d.NewThreads + d.QuizSessions).ToList()));

        html.AppendLine("<h2>Aktivnost po sedmicama</h2>");
        html.AppendLine(BarChartSvg(
            weekly.Select(w => w.Start.ToString("dd.MM.", CultureInfo.InvariantCulture)).ToList(),
            weekly.Select(w => w.Registrations + w.Reviews + w.Threads + w.Quiz).ToList()));
        html.AppendLine("<table class='stats-table'>");
        html.AppendLine("<thead><tr><th>Sedmica od</th><th>Dana</th><th>Registracije</th><th>Recenzije</th><th>Threadovi</th><th>Kviz sesije</th><th>Prosjek po danu</th></tr></thead><tbody>");
        foreach (var w in weekly)
        {
            var total = w.Registrations + w.Reviews + w.Threads + w.Quiz;
            html.AppendLine($"<tr><td>{w.Start:dd.MM.yyyy}</td><td>{w.Days}</td><td>{w.Registrations}</td><td>{w.Reviews}</td><td>{w.Threads}</td><td>{w.Quiz}</td><td>{(total / (double)w.Days).ToString("0.00", CultureInfo.InvariantCulture)}</td></tr>");
        }

        html.AppendLine("</tbody></table>");

        html.AppendLine("<h2>Dnevni pregled</h2>");
        html.AppendLine("<table class='stats-table'>");
        html.AppendLine("<thead><tr><th>Datum</th><th>Nove registracije</th><th>Nove recenzije</th><th>Novi threadovi</th><th>Kviz sesije</th></tr></thead><tbody>");
        foreach (var day in dailyStats)
        {
            html.AppendLine($"<tr><td>{day.Date:dd.MM.yyyy}</td><td>{day.NewRegistrations}</td><td>{day.NewReviews}</td><td>{day.NewThreads}</td><td>{day.QuizSessions}</td></tr>");
        }

        html.AppendLine("</tbody></table>");
        html.AppendLine("</div>");

        return await pdfGenerator.GeneratePdfFromHtmlAsync(html.ToString(), GetReportStyles(), new(), cancellationToken);
    }

    public async Task<byte[]> GenerateContentPopularityReportPdfAsync(
        DateTime startDate,
        DateTime endDate,
        List<(string Title, int ReviewCount, double AvgRating, int WatchlistCount, int ThreadCount)> topMovies,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating Content Popularity Report PDF for {Start} - {End}", startDate, endDate);

        var html = new StringBuilder();
        html.AppendLine("<div class='report-container'>");
        html.AppendLine("<h1 class='report-title'>IZVJEŠTAJ POPULARNOSTI SADRŽAJA</h1>");
        html.AppendLine($"<p class='report-subtitle'>Period: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}</p>");
        html.AppendLine($"<p class='report-date'>Generirano: {DateTime.UtcNow:dd.MM.yyyy HH:mm}</p>");

        if (topMovies.Count == 0)
        {
            html.AppendLine("<p>U odabranom periodu nema aktivnosti na naslovima.</p>");
        }
        else
        {
            html.AppendLine("<h2>Najpopularniji naslovi (recenzije + watchlist + diskusije u periodu)</h2>");
            html.AppendLine(BarChartSvg(
                topMovies.Select(m => m.Title.Length > 14 ? m.Title[..13] + "…" : m.Title).ToList(),
                topMovies.Select(m => m.ReviewCount + m.WatchlistCount + m.ThreadCount).ToList()));
        }

        html.AppendLine("<table class='stats-table'>");
        html.AppendLine("<thead><tr><th>#</th><th>Naslov</th><th>Broj recenzija</th><th>Prosječna ocjena</th><th>Watchlist dodavanja</th><th>Forum diskusije</th></tr></thead><tbody>");

        var rank = 1;
        foreach (var movie in topMovies)
        {
            html.AppendLine($"<tr><td>{rank}.</td><td>{E(movie.Title)}</td><td>{movie.ReviewCount}</td><td>{movie.AvgRating.ToString("0.0", CultureInfo.InvariantCulture)}</td><td>{movie.WatchlistCount}</td><td>{movie.ThreadCount}</td></tr>");
            rank++;
        }

        html.AppendLine("</tbody></table>");
        html.AppendLine("</div>");

        return await pdfGenerator.GeneratePdfFromHtmlAsync(html.ToString(), GetReportStyles(), new(), cancellationToken);
    }

    private static string BarChartSvg(List<string> labels, List<int> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        const int width = 720, height = 220, left = 36, bottom = 28, top = 14;
        var max = Math.Max(1, values.Max());
        var plotW = width - left - 8;
        var plotH = height - bottom - top;
        var slot = plotW / (double)values.Count;
        var barW = Math.Max(1.5, slot * 0.7);
        var labelEvery = Math.Max(1, (int)Math.Ceiling(values.Count / 16.0));
        var inv = CultureInfo.InvariantCulture;

        var svg = new StringBuilder();
        svg.Append($"<svg class='chart' viewBox='0 0 {width} {height}' width='100%' xmlns='http://www.w3.org/2000/svg'>");
        svg.Append($"<line x1='{left}' y1='{top}' x2='{left}' y2='{top + plotH}' stroke='#999'/>");
        svg.Append($"<line x1='{left}' y1='{top + plotH}' x2='{width - 8}' y2='{top + plotH}' stroke='#999'/>");
        svg.Append($"<text x='{left - 4}' y='{top + 8}' font-size='10' text-anchor='end'>{max}</text>");
        svg.Append($"<text x='{left - 4}' y='{top + plotH}' font-size='10' text-anchor='end'>0</text>");
        for (var i = 0; i < values.Count; i++)
        {
            var h = values[i] / (double)max * plotH;
            var x = left + i * slot + (slot - barW) / 2;
            svg.Append($"<rect x='{x.ToString("0.##", inv)}' y='{(top + plotH - h).ToString("0.##", inv)}' width='{barW.ToString("0.##", inv)}' height='{h.ToString("0.##", inv)}' fill='#1a5490'><title>{E(labels[i])}: {values[i]}</title></rect>");
            if (i % labelEvery == 0)
            {
                svg.Append($"<text x='{(x + barW / 2).ToString("0.##", inv)}' y='{height - 12}' font-size='9' text-anchor='middle'>{E(labels[i])}</text>");
            }
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string GetReportStyles() => @"
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: 'Segoe UI', sans-serif; color: #333; }
.report-container { padding: 20px; }
.report-title { color: #1a5490; font-size: 28px; text-align: center; border-bottom: 3px solid #1a5490; padding-bottom: 15px; margin-bottom: 10px; }
.report-subtitle { text-align: center; color: #666; font-size: 14px; margin-bottom: 5px; }
.report-date { text-align: right; color: #999; font-size: 12px; margin-bottom: 25px; font-style: italic; }
.stats-table { width: 100%; border-collapse: collapse; margin-bottom: 25px; }
.stats-table th { background-color: #1a5490; color: white; padding: 10px; text-align: left; }
.stats-table td { padding: 8px 10px; border-bottom: 1px solid #e0e0e0; }
.stats-table tr:nth-child(even) { background-color: #f9f9f9; }
.stat-value { font-weight: bold; color: #1a5490; }
.chart { margin-bottom: 20px; }
h2 { color: #1a5490; margin: 20px 0 10px 0; }
";
}
