import 'dart:async';

import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class _DashboardData {
  _DashboardData(this.kpis, this.popular, this.threads, this.newUsers);

  final Kpis kpis;
  final List<PopularMovie> popular;
  final List<ActiveThread> threads;
  final List<AppUser> newUsers;
}

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  _Range _range = _Range.days7;
  _DashboardData? _data;
  Object? _error;
  bool _loading = true;
  int _tick = 0;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _refresh();
    _timer = Timer.periodic(const Duration(seconds: 30), (_) => _refresh(silent: true));
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _refresh({bool silent = false}) async {
    if (_busy) return;
    _busy = true;
    if (!silent && mounted) setState(() => _loading = _data == null);
    try {
      final data = await _load(context.read<Repository>());
      if (mounted) {
        setState(() {
          _data = data;
          _error = null;
          _tick++;
        });
      }
    } catch (e) {
      if (mounted && _data == null) setState(() => _error = e);
    } finally {
      _busy = false;
      if (mounted) setState(() => _loading = false);
    }
  }

  bool _busy = false;

  Future<_DashboardData> _load(Repository repo) async {
    final results = await Future.wait([
      repo.kpis(),
      repo.popularMovies(10),
      repo.activeThreads(4),
      repo.users(pageSize: 100),
    ]);
    final users = (results[3] as Paged<AppUser>).items
      ..sort((a, b) => b.registrationDate.compareTo(a.registrationDate));
    return _DashboardData(results[0] as Kpis, results[1] as List<PopularMovie>,
        results[2] as List<ActiveThread>, users.take(5).toList());
  }

  @override
  Widget build(BuildContext context) {
    if (_loading && _data == null) return const Center(child: CircularProgressIndicator());
    if (_data == null) return ErrorView(message: '$_error', onRetry: _refresh);
    final data = _data!;
    return ListView(
        children: [
          PageHeader(
            title: 'Overview',
            subtitle: 'Metrics refresh automatically every 30 seconds.',
            actions: [
              OutlinedButton.icon(
                onPressed: _refresh,
                icon: const Icon(Icons.refresh, size: 18),
                label: const Text('Refresh'),
              ),
            ],
          ),
          _KpiRow(kpis: data.kpis),
          const SizedBox(height: 20),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: Panel(
                  title: 'Platform activity',
                  trailing: SegmentedButton<_Range>(
                    showSelectedIcon: false,
                    segments: const [
                      ButtonSegment(value: _Range.days7, label: Text('7 days')),
                      ButtonSegment(value: _Range.days30, label: Text('30 days')),
                      ButtonSegment(value: _Range.weeks12, label: Text('12 weeks')),
                    ],
                    selected: {_range},
                    onSelectionChanged: (s) => setState(() => _range = s.first),
                  ),
                  child: SizedBox(height: 280, child: _ActivityChart(range: _range, tick: _tick)),
                ),
              ),
              const SizedBox(width: 20),
              Expanded(
                flex: 2,
                child: Panel(
                  title: 'Top 10 titles',
                  child: Column(children: [
                    if (data.popular.isEmpty)
                      const Padding(
                        padding: EdgeInsets.all(16),
                        child: Text('No activity yet.', style: TextStyle(color: AppColors.muted)),
                      ),
                    for (var i = 0; i < data.popular.length; i++)
                      _TrendingTile(rank: i + 1, movie: data.popular[i]),
                  ]),
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: Panel(
                  title: 'Hottest discussions',
                  child: Column(children: [
                    if (data.threads.isEmpty)
                      const Padding(
                        padding: EdgeInsets.all(16),
                        child: Text('No discussions yet.', style: TextStyle(color: AppColors.muted)),
                      ),
                    for (final t in data.threads) _ThreadTile(thread: t),
                  ]),
                ),
              ),
              const SizedBox(width: 20),
              Expanded(
                flex: 2,
                child: Panel(
                  title: 'New users',
                  child: Column(children: [
                    for (final u in data.newUsers) _NewUserTile(user: u),
                  ]),
                ),
              ),
            ],
          ),
        ],
      );
  }
}

enum _Range { days7, days30, weeks12 }

class _KpiRow extends StatelessWidget {
  const _KpiRow({required this.kpis});

  final Kpis kpis;

  @override
  Widget build(BuildContext context) {
    final tiles = [
      _Kpi('Users', kpis.totalUsers, Icons.people_outline, kpis.usersTrend),
      _Kpi('New users (7d)', kpis.newUsers, Icons.person_add_alt, null),
      _Kpi('Reviews', kpis.totalReviews, Icons.rate_review_outlined, kpis.reviewsTrend),
      _Kpi('Threads', kpis.activeThreads, Icons.forum_outlined, kpis.threadsTrend),
      _Kpi('Titles', kpis.totalMovies, Icons.movie_outlined, null),
      _Kpi('Quiz sessions', kpis.quizSessions, Icons.quiz_outlined, kpis.quizTrend),
    ];
    return Row(children: [
      for (var i = 0; i < tiles.length; i++) ...[
        if (i > 0) const SizedBox(width: 12),
        Expanded(child: tiles[i]),
      ],
    ]);
  }
}

class _Kpi extends StatelessWidget {
  const _Kpi(this.label, this.value, this.icon, this.trend);

  final String label;
  final int value;
  final IconData icon;
  final int? trend;

  @override
  Widget build(BuildContext context) {
    final up = (trend ?? 0) >= 0;
    final color = up ? AppColors.green : AppColors.red;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Icon(icon, size: 16, color: AppColors.muted),
              const SizedBox(width: 6),
              Expanded(
                child: Text(label,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: AppColors.muted, fontSize: 12)),
              ),
            ]),
            const SizedBox(height: 10),
            Text(value.toString(),
                style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            if (trend != null)
              Row(children: [
                Icon(up ? Icons.trending_up : Icons.trending_down, size: 14, color: color),
                const SizedBox(width: 4),
                Text('${up ? '+' : ''}$trend%', style: TextStyle(color: color, fontSize: 12)),
              ])
            else
              const SizedBox(height: 16),
          ],
        ),
      ),
    );
  }
}

class _ActivityChart extends StatefulWidget {
  const _ActivityChart({required this.range, required this.tick});

  final _Range range;
  final int tick;

  @override
  State<_ActivityChart> createState() => _ActivityChartState();
}

class _ActivityChartState extends State<_ActivityChart> {
  List<ActivityPoint>? _points;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant _ActivityChart old) {
    super.didUpdateWidget(old);
    if (old.range != widget.range) _points = null;
    if (old.range != widget.range || old.tick != widget.tick) _load();
  }

  Future<void> _load() async {
    final range = widget.range;
    try {
      final days = range == _Range.weeks12 ? 84 : (range == _Range.days30 ? 30 : 7);
      var points = await context.read<Repository>().activity(days);
      if (range == _Range.weeks12) points = _byWeek(points);
      if (mounted && range == widget.range) {
        setState(() {
          _points = points;
          _error = null;
        });
      }
    } catch (e) {
      if (mounted && _points == null) setState(() => _error = e);
    }
  }

  static List<ActivityPoint> _byWeek(List<ActivityPoint> days) {
    final weeks = <DateTime, List<ActivityPoint>>{};
    for (final d in days) {
      final date = DateTime(d.date.year, d.date.month, d.date.day);
      final monday = date.subtract(Duration(days: date.weekday - 1));
      weeks.putIfAbsent(monday, () => []).add(d);
    }
    final keys = weeks.keys.toList()..sort();
    return [
      for (final k in keys)
        ActivityPoint({
          'date': k.toIso8601String(),
          'newUsers': weeks[k]!.fold<int>(0, (a, p) => a + p.newUsers),
          'reviews': weeks[k]!.fold<int>(0, (a, p) => a + p.reviews),
          'threads': weeks[k]!.fold<int>(0, (a, p) => a + p.threads),
          'comments': weeks[k]!.fold<int>(0, (a, p) => a + p.comments),
          'quizSessions': weeks[k]!.fold<int>(0, (a, p) => a + p.quizSessions),
        })
    ];
  }

  @override
  Widget build(BuildContext context) {
    final points = _points;
    if (points == null) {
      return _error != null ? ErrorView(message: '$_error', onRetry: _load) : const Center(child: CircularProgressIndicator());
    }
    {
        List<FlSpot> spots(int Function(ActivityPoint) pick) => [
              for (var i = 0; i < points.length; i++)
                FlSpot(i.toDouble(), pick(points[i]).toDouble()),
            ];
        LineChartBarData line(Color c, List<FlSpot> s) => LineChartBarData(
              spots: s,
              color: c,
              barWidth: 3,
              isCurved: true,
              dotData: const FlDotData(show: false),
            );
        final step = points.length <= 7 ? 1 : (points.length / 6).ceil();
        return Column(children: [
          Expanded(
            child: LineChart(LineChartData(
              minY: 0,
              gridData: FlGridData(
                drawVerticalLine: false,
                getDrawingHorizontalLine: (_) =>
                    const FlLine(color: AppColors.border, strokeWidth: 1),
              ),
              borderData: FlBorderData(show: false),
              titlesData: FlTitlesData(
                topTitles: const AxisTitles(),
                rightTitles: const AxisTitles(),
                leftTitles: const AxisTitles(
                  sideTitles: SideTitles(showTitles: true, reservedSize: 32),
                ),
                bottomTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    interval: step.toDouble(),
                    getTitlesWidget: (v, meta) {
                      final i = v.toInt();
                      if (i < 0 || i >= points.length) return const SizedBox.shrink();
                      final d = points[i].date;
                      return Padding(
                        padding: const EdgeInsets.only(top: 6),
                        child: Text('${d.day}.${d.month}.',
                            style: const TextStyle(color: AppColors.muted, fontSize: 11)),
                      );
                    },
                  ),
                ),
              ),
              lineBarsData: [
                line(AppColors.red, spots((p) => p.reviews)),
                line(Colors.white, spots((p) => p.threads)),
                line(AppColors.gold, spots((p) => p.quizSessions)),
              ],
            )),
          ),
          const SizedBox(height: 12),
          const Row(mainAxisAlignment: MainAxisAlignment.center, children: [
            _Legend(AppColors.red, 'Reviews'),
            SizedBox(width: 20),
            _Legend(Colors.white, 'Threads'),
            SizedBox(width: 20),
            _Legend(AppColors.gold, 'Quizzes'),
          ]),
        ]);
    }
  }
}

class _Legend extends StatelessWidget {
  const _Legend(this.color, this.label);

  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) => Row(children: [
        Container(
            width: 10, height: 10, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
        const SizedBox(width: 6),
        Text(label, style: const TextStyle(color: AppColors.muted, fontSize: 12)),
      ]);
}

class _TrendingTile extends StatelessWidget {
  const _TrendingTile({required this.rank, required this.movie});

  final int rank;
  final PopularMovie movie;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: AppColors.surfaceHigh,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(children: [
        Text('#$rank', style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700)),
        const SizedBox(width: 12),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(movie.title,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontWeight: FontWeight.w700)),
            Text('${movie.reviewCount} reviews · ${movie.watchlistCount + movie.interestedCount} saved',
                style: const TextStyle(color: AppColors.muted, fontSize: 12)),
          ]),
        ),
        const Icon(Icons.star, size: 16, color: AppColors.gold),
        const SizedBox(width: 4),
        Text(movie.averageRating.toStringAsFixed(1),
            style: const TextStyle(color: AppColors.gold, fontWeight: FontWeight.w700)),
      ]),
    );
  }
}

class _ThreadTile extends StatelessWidget {
  const _ThreadTile({required this.thread});

  final ActiveThread thread;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.surfaceHigh,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(thread.title, style: const TextStyle(fontWeight: FontWeight.w700)),
        const SizedBox(height: 6),
        Text('@${thread.author} · ${thread.commentCount} replies · ${timeAgo(thread.createdAt)}',
            style: const TextStyle(color: AppColors.muted, fontSize: 12)),
      ]),
    );
  }
}

class _NewUserTile extends StatelessWidget {
  const _NewUserTile({required this.user});

  final AppUser user;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: CircleAvatar(
        backgroundColor: AppColors.surfaceHigh,
        child: Text(user.username.isEmpty ? '?' : user.username[0].toUpperCase()),
      ),
      title: Text(user.fullName),
      subtitle: Text('Joined ${timeAgo(user.registrationDate)}',
          style: const TextStyle(color: AppColors.muted)),
    );
  }
}
