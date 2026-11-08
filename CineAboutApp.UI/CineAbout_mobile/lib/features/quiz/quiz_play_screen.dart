import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class QuizPlayScreen extends StatefulWidget {
  const QuizPlayScreen({super.key, required this.quiz});

  final QuizStart quiz;

  @override
  State<QuizPlayScreen> createState() => _QuizPlayScreenState();
}

class _QuizPlayScreenState extends State<QuizPlayScreen> {
  static const _letters = ['A', 'B', 'C', 'D'];

  final List<Map<String, dynamic>> _answers = [];
  final Map<int, String> _picked = {};
  final Stopwatch _watch = Stopwatch();
  Timer? _ticker;
  int _index = 0;
  int _left = 0;
  String? _selected;
  bool _submitting = false;
  QuizResult? _result;
  String? _error;

  QuizStart get _quiz => widget.quiz;

  @override
  void initState() {
    super.initState();
    _startQuestion();
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  void _startQuestion() {
    _ticker?.cancel();
    _left = _quiz.secondsPerQuestion;
    _selected = null;
    _watch
      ..reset()
      ..start();
    _ticker = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) return;
      setState(() => _left--);
      if (_left <= 0) _answer('');
    });
  }

  void _answer(String letter) {
    if (_selected != null) return;
    _ticker?.cancel();
    _watch.stop();
    final seconds = (_watch.elapsedMilliseconds / 1000).clamp(0, 120).toDouble();
    final q = _quiz.questions[_index];
    _answers.add({'questionId': q.id, 'selectedOption': letter, 'responseSeconds': seconds});
    _picked[q.id] = letter;
    setState(() => _selected = letter);
    Future.delayed(const Duration(milliseconds: 450), () {
      if (!mounted) return;
      if (_index + 1 < _quiz.questions.length) {
        setState(() => _index++);
        _startQuestion();
      } else {
        _submit();
      }
    });
  }

  Future<void> _submit() async {
    final repo = context.read<Repository>();
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      final result = await repo.submitQuiz(_quiz.sessionId, _answers);
      if (mounted) setState(() => _result = result);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_result != null) return _ResultView(quiz: _quiz, result: _result!, picked: _picked);
    if (_submitting || _error != null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Quiz')),
        body: _error != null
            ? ErrorView(message: _error!, onRetry: _submit)
            : const Center(child: CircularProgressIndicator()),
      );
    }
    final q = _quiz.questions[_index];
    final fraction = _left / _quiz.secondsPerQuestion;
    return Scaffold(
      appBar: AppBar(
        title: Text('Question ${_index + 1}/${_quiz.questions.length}'),
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () async {
            final yes = await confirm(context,
                title: 'Leave quiz', message: 'Your progress will be lost.', confirmLabel: 'Leave');
            if (yes && context.mounted) context.pop();
          },
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: LinearProgressIndicator(
              value: fraction.clamp(0, 1),
              minHeight: 8,
              color: fraction < 0.25 ? AppColors.red : AppColors.gold,
              backgroundColor: AppColors.border,
            ),
          ),
          const SizedBox(height: 8),
          Align(
            alignment: Alignment.centerRight,
            child: Text('$_left s', style: const TextStyle(color: AppColors.muted)),
          ),
          const SizedBox(height: 12),
          Chip2(q.category, color: AppColors.red),
          const SizedBox(height: 12),
          Text(q.text, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800, height: 1.3)),
          const SizedBox(height: 24),
          for (var i = 0; i < 4; i++)
            if (q.options[i].isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: OutlinedButton(
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size.fromHeight(56),
                    alignment: Alignment.centerLeft,
                    backgroundColor: _selected == _letters[i] ? AppColors.redDark : null,
                    side: BorderSide(
                        color: _selected == _letters[i] ? AppColors.red : AppColors.border),
                  ),
                  onPressed: _selected == null ? () => _answer(_letters[i]) : null,
                  child: Row(children: [
                    Text('${_letters[i]}.  ',
                        style: const TextStyle(color: AppColors.red, fontWeight: FontWeight.w900)),
                    Expanded(child: Text(q.options[i], style: const TextStyle(fontSize: 16))),
                  ]),
                ),
              ),
        ]),
      ),
    );
  }
}

class _ResultView extends StatelessWidget {
  const _ResultView({required this.quiz, required this.result, required this.picked});

  final QuizStart quiz;
  final QuizResult result;
  final Map<int, String> picked;

  @override
  Widget build(BuildContext context) {
    final byId = {for (final a in result.answers) a.questionId: a};
    return Scaffold(
      appBar: AppBar(title: const Text('Results')),
      body: ListView(padding: const EdgeInsets.all(16), children: [
        Container(
          padding: const EdgeInsets.all(24),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: AppColors.border),
          ),
          child: Column(children: [
            const Icon(Icons.check_circle, size: 56, color: AppColors.red),
            const SizedBox(height: 12),
            Text('${result.correctCount}/${result.questionCount}',
                style: const TextStyle(fontSize: 40, fontWeight: FontWeight.w900)),
            const Text('Correct answers', style: TextStyle(color: AppColors.muted)),
            const SizedBox(height: 14),
            Wrap(spacing: 8, children: [
              Chip2('+${result.score} points', color: AppColors.gold),
              if (result.streak > 0) Chip2('Streak: ${result.streak} days', color: AppColors.amber),
            ]),
          ]),
        ),
        const SectionTitle('Quiz review'),
        for (final q in quiz.questions)
          Builder(builder: (context) {
            final a = byId[q.id];
            final correct = a?.isCorrect ?? false;
            final mine = picked[q.id] ?? '';
            String text(String letter) {
              final i = 'ABCD'.indexOf(letter);
              return i < 0 ? '—' : '$letter. ${q.options[i]}';
            }

            return Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Icon(correct ? Icons.check_circle : Icons.cancel,
                        color: correct ? AppColors.green : AppColors.red, size: 22),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(q.text, style: const TextStyle(fontWeight: FontWeight.w700)),
                        const SizedBox(height: 6),
                        Text('Your answer: ${mine.isEmpty ? 'no answer' : text(mine)}',
                            style: const TextStyle(color: AppColors.muted, fontSize: 13)),
                        if (!correct && a != null)
                          Text('Correct answer: ${text(a.correctOption)}',
                              style: const TextStyle(color: AppColors.green, fontSize: 13)),
                      ]),
                    ),
                  ]),
                ),
              ),
            );
          }),
        const SizedBox(height: 8),
        FilledButton(
          onPressed: () => context.pushReplacement('/leaderboard'),
          child: const Text('View leaderboard'),
        ),
        const SizedBox(height: 10),
        OutlinedButton(onPressed: () => context.pop(), child: const Text('Back to quiz')),
      ]),
    );
  }
}
