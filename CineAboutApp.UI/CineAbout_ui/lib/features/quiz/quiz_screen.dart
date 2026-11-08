import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/paged_table.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class QuizScreen extends StatefulWidget {
  const QuizScreen({super.key});

  @override
  State<QuizScreen> createState() => _QuizScreenState();
}

class _QuizScreenState extends State<QuizScreen> {
  final _table = PagedTableController();
  String _search = '';
  String? _difficulty;
  String? _category;

  Future<void> _edit([QuizQuestion? q]) async {
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => _QuestionDialog(question: q),
    );
    if (saved == true) _table.reload();
  }

  Future<void> _delete(QuizQuestion q) async {
    final ok = await confirm(context,
        title: 'Delete question',
        message: 'Delete "${q.text}"?',
        confirmLabel: 'Delete',
        destructive: true);
    if (!ok || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.deleteQuestion(q.id), success: 'Question deleted.')) {
      _table.reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(children: [
      PageHeader(
        title: 'Quiz questions',
        subtitle: 'Manage, edit and organize the cinematic question bank.',
        actions: [
          FilledButton.icon(
            onPressed: _edit,
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Add question'),
          ),
        ],
      ),
      Wrap(spacing: 12, runSpacing: 12, children: [
        SearchBox(
          hint: 'Search by question text',
          onChanged: (v) {
            _search = v;
            _table.reset();
          },
        ),
        SizedBox(
          width: 190,
          child: TextField(
            decoration: const InputDecoration(labelText: 'Category'),
            onChanged: (v) {
              _category = v.trim().isEmpty ? null : v.trim();
              _table.reset();
            },
          ),
        ),
        FilterDropdown<String>(
          label: 'Difficulty',
          value: _difficulty,
          options: const {'Easy': 'Easy', 'Medium': 'Medium', 'Hard': 'Hard'},
          onChanged: (v) {
            setState(() => _difficulty = v);
            _table.reset();
          },
        ),
      ]),
      const SizedBox(height: 16),
      PagedTable<QuizQuestion>(
        controller: _table,
        fetch: (page) => repo.questions(
            search: _search, category: _category, difficulty: _difficulty, page: page),
        columns: const [
          DataColumnSpec('Question', flex: 4),
          DataColumnSpec('Category', width: 130),
          DataColumnSpec('Difficulty', width: 100),
          DataColumnSpec('Answer', width: 80),
          DataColumnSpec('', width: 90),
        ],
        emptyText: 'No questions match the filters.',
        onRowTap: _edit,
        rowBuilder: (q) => [
          Text(q.text, maxLines: 2, overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w700)),
          Text(q.category),
          StatusChip(q.difficulty),
          Text(q.correct),
          Row(mainAxisSize: MainAxisSize.min, children: [
            IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _edit(q)),
            IconButton(
                tooltip: 'Delete',
                icon: const Icon(Icons.delete_outline, size: 18),
                onPressed: () => _delete(q)),
          ]),
        ],
      ),
    ]);
  }
}

class _QuestionDialog extends StatefulWidget {
  const _QuestionDialog({this.question});

  final QuizQuestion? question;

  @override
  State<_QuestionDialog> createState() => _QuestionDialogState();
}

class _QuestionDialogState extends State<_QuestionDialog> {
  static const _letters = ['A', 'B', 'C', 'D'];

  final _form = GlobalKey<FormState>();
  late final _text = TextEditingController(text: widget.question?.text);
  int? _categoryId;
  List<Named> _categories = const [];
  late final List<TextEditingController> _options = [
    for (var i = 0; i < 4; i++) TextEditingController(text: widget.question?.options[i]),
  ];
  late String _correct = widget.question?.correct.isNotEmpty == true ? widget.question!.correct : 'A';
  late String _difficulty =
      widget.question?.difficulty.isNotEmpty == true ? widget.question!.difficulty : 'Easy';
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _categoryId = widget.question?.categoryId;
    context.read<Repository>().reference('QuizCategory').then((items) {
      if (!mounted) return;
      setState(() {
        _categories = items;
        if (!items.any((c) => c.id == _categoryId)) _categoryId = null;
      });
    });
  }

  @override
  void dispose() {
    _text.dispose();
    for (final c in _options) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    final repo = context.read<Repository>();
    setState(() => _saving = true);
    final ok = await runAction(
      context,
      () => repo.saveQuestion(widget.question?.id, {
        'text': _text.text.trim(),
        'optionA': _options[0].text.trim(),
        'optionB': _options[1].text.trim(),
        'optionC': _options[2].text.trim(),
        'optionD': _options[3].text.trim(),
        'correctOption': _correct,
        'categoryId': _categoryId,
        'difficulty': _difficulty,
      }),
      success: 'Question saved.',
    );
    if (!mounted) return;
    setState(() => _saving = false);
    if (ok) Navigator.pop(context, true);
  }

  String? _required(String? v) => (v == null || v.trim().isEmpty) ? 'Required' : null;

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: DialogTitle(widget.question == null ? 'Add question' : 'Edit question'),
      content: SizedBox(
        width: 540,
        child: Form(
          key: _form,
          child: SingleChildScrollView(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              TextFormField(
                controller: _text,
                minLines: 2,
                maxLines: 4,
                decoration: const InputDecoration(labelText: 'Question *'),
                validator: _required,
              ),
              const SizedBox(height: 12),
              RadioGroup<String>(
                groupValue: _correct,
                onChanged: (v) => setState(() => _correct = v ?? 'A'),
                child: Column(children: [
              for (var i = 0; i < 4; i++)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: Row(children: [
                    Radio<String>(
                      value: _letters[i],
                      activeColor: AppColors.red,
                    ),
                    Expanded(
                      child: TextFormField(
                        controller: _options[i],
                        decoration: InputDecoration(labelText: 'Answer ${_letters[i]} *'),
                        validator: _required,
                      ),
                    ),
                  ]),
                ),
                ]),
              ),
              const Align(
                alignment: Alignment.centerLeft,
                child: Text('Select the radio button next to the correct answer.',
                    style: TextStyle(color: AppColors.muted, fontSize: 12)),
              ),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: DropdownButtonFormField<int>(
                    key: ValueKey('cat-${_categories.length}-$_categoryId'),
                    initialValue: _categoryId,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Category *'),
                    items: [for (final c in _categories) DropdownMenuItem(value: c.id, child: Text(c.name))],
                    onChanged: (v) => setState(() => _categoryId = v),
                    validator: (v) => v == null ? 'Required' : null,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _difficulty,
                    decoration: const InputDecoration(labelText: 'Difficulty'),
                    items: const [
                      DropdownMenuItem(value: 'Easy', child: Text('Easy')),
                      DropdownMenuItem(value: 'Medium', child: Text('Medium')),
                      DropdownMenuItem(value: 'Hard', child: Text('Hard')),
                    ],
                    onChanged: (v) => setState(() => _difficulty = v ?? 'Easy'),
                  ),
                ),
              ]),
            ]),
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        FilledButton(onPressed: _saving ? null : _save, child: const Text('Save')),
      ],
    );
  }
}
