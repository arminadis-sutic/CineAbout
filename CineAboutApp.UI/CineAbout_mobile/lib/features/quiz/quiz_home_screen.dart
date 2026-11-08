import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class _QuizHomeData {
  _QuizHomeData(this.streak, this.categories);

  final Streak streak;
  final List<String> categories;
}

class QuizHomeScreen extends StatefulWidget {
  const QuizHomeScreen({super.key});

  @override
  State<QuizHomeScreen> createState() => _QuizHomeScreenState();
}

class _QuizHomeScreenState extends State<QuizHomeScreen> {
  int _version = 0;
  bool _starting = false;

  Future<void> _start(Future<QuizStart> Function() begin) async {
    setState(() => _starting = true);
    try {
      final quiz = await begin();
      if (!mounted) return;
      await context.push('/quiz/play', extra: quiz);
      if (mounted) setState(() => _version++);
    } catch (e) {
      if (mounted) showMessage(context, e.toString(), error: true);
    } finally {
      if (mounted) setState(() => _starting = false);
    }
  }

  Future<void> _freeQuiz(Repository repo, String category) async {
    var difficulty = 'Any';
    var count = 5;
    final go = await showModalBottomSheet<bool>(
      context: context,
      backgroundColor: AppColors.surface,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setSheet) => SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(category[0].toUpperCase() + category.substring(1),
                  style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w900)),
              const SizedBox(height: 16),
              const Text('Difficulty', style: TextStyle(color: AppColors.muted)),
              const SizedBox(height: 8),
              Wrap(spacing: 8, children: [
                for (final d in const ['Any', 'Easy', 'Medium', 'Hard'])
                  ChoiceChip(
                    label: Text(d),
                    selected: difficulty == d,
                    onSelected: (_) => setSheet(() => difficulty = d),
                  ),
              ]),
              const SizedBox(height: 16),
              Text('Questions: $count', style: const TextStyle(color: AppColors.muted)),
              Slider(
                value: count.toDouble(),
                min: 3,
                max: 20,
                divisions: 17,
                activeColor: AppColors.red,
                onChanged: (v) => setSheet(() => count = v.round()),
              ),
              FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Start')),
            ]),
          ),
        ),
      ),
    );
    if (go != true || !mounted) return;
    await _start(() => repo.startFreeQuiz(
        category: category, difficulty: difficulty == 'Any' ? null : difficulty, count: count));
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(title: const Text('Quiz')),
      body: AsyncData<_QuizHomeData>(
        key: ValueKey(_version),
        load: () async {
          final r = await Future.wait([repo.myStreak(), repo.quizCategories()]);
          return _QuizHomeData(r[0] as Streak, r[1] as List<String>);
        },
        builder: (context, data, reload) => ListView(padding: const EdgeInsets.all(16), children: [
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              gradient: const LinearGradient(colors: [Color(0xFF3A1114), Color(0xFF181818)]),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppColors.border),
            ),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Wrap(spacing: 8, children: [
                Chip2('${data.streak.current} day streak', color: AppColors.amber),
                Chip2('Best ${data.streak.longest}', color: AppColors.muted),
              ]),
              const SizedBox(height: 14),
              const Text('Daily quiz', style: TextStyle(fontSize: 28, fontWeight: FontWeight.w900)),
              const SizedBox(height: 6),
              Text(
                data.streak.playedToday
                    ? 'You already played today. Come back tomorrow to keep your streak!'
                    : 'Same questions for everyone. Answer fast for bonus points!',
                style: const TextStyle(color: AppColors.muted),
              ),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: _starting || data.streak.playedToday ? null : () => _start(repo.startDailyQuiz),
                child: Text(data.streak.playedToday ? 'Completed today' : 'Start'),
              ),
            ]),
          ),
          const SectionTitle('Categories'),
          GridView.count(
            crossAxisCount: 2,
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            mainAxisSpacing: 12,
            crossAxisSpacing: 12,
            childAspectRatio: 1.6,
            children: [
              for (final c in data.categories)
                Card(
                  clipBehavior: Clip.antiAlias,
                  child: InkWell(
                    onTap: _starting ? null : () => _freeQuiz(repo, c),
                    child: Padding(
                      padding: const EdgeInsets.all(14),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Icon(Icons.local_movies_outlined, color: AppColors.red),
                          Text(c[0].toUpperCase() + c.substring(1),
                              style: const TextStyle(fontWeight: FontWeight.w800)),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 16),
          Card(
            child: ListTile(
              leading: const Icon(Icons.leaderboard, color: AppColors.gold),
              title: const Text('Global leaderboard', style: TextStyle(fontWeight: FontWeight.w800)),
              subtitle: const Text('See where you rank globally'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push('/leaderboard'),
            ),
          ),
        ]),
      ),
    );
  }
}
