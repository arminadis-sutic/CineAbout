import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/repository.dart';
import '../../core/widgets.dart';

Future<void> showReportDialog(BuildContext context,
    {required String targetType, required int targetId}) async {
  final repo = context.read<Repository>();
  final reason = TextEditingController();
  final form = GlobalKey<FormState>();
  final submit = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text('Report ${targetType.toLowerCase()}'),
      content: Form(
        key: form,
        child: TextFormField(
          controller: reason,
          maxLength: 500,
          minLines: 2,
          maxLines: 4,
          autofocus: true,
          decoration: const InputDecoration(hintText: 'What is wrong with this content?'),
          validator: (v) =>
              (v == null || v.trim().length < 3) ? 'Please describe the problem' : null,
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
        TextButton(
          onPressed: () {
            if (form.currentState!.validate()) Navigator.pop(ctx, true);
          },
          child: const Text('Report'),
        ),
      ],
    ),
  );
  final text = reason.text.trim();
  reason.dispose();
  if (submit != true || !context.mounted) return;
  await runAction(context, () => repo.report(targetType, targetId, text),
      success: 'Thanks, our moderators will take a look.');
}
