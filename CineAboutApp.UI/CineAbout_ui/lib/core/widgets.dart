import 'dart:async';

import 'package:flutter/material.dart';

import 'config.dart';
import 'theme.dart';

void showMessage(BuildContext context, String message, {bool error = false}) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(
      backgroundColor: error ? const Color(0xFF7A1C22) : const Color(0xFF1E6B3A),
      duration: const Duration(seconds: 4),
      width: 460,
      content: Row(children: [
        Icon(error ? Icons.error_outline : Icons.check_circle_outline, color: Colors.white, size: 22),
        const SizedBox(width: 12),
        Expanded(
          child: Text(message,
              style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600)),
        ),
      ]),
    ));
}

class DialogTitle extends StatelessWidget {
  const DialogTitle(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      Expanded(child: Text(text)),
      IconButton(
        tooltip: 'Close',
        icon: const Icon(Icons.close),
        onPressed: () => Navigator.of(context).pop(),
      ),
    ]);
  }
}

class Thumb extends StatelessWidget {
  const Thumb({super.key, required this.url, required this.label, this.round = false, this.size = 40});

  final String? url;
  final String label;
  final bool round;
  final double size;

  @override
  Widget build(BuildContext context) {
    final resolved = AppConfig.mediaUrl(url);
    final height = round ? size : size * 1.45;
    final fallback = Container(
      width: size,
      height: height,
      alignment: Alignment.center,
      color: AppColors.surfaceHigh,
      child: Text(label.isEmpty ? '?' : label[0].toUpperCase(),
          style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w800)),
    );
    final image = resolved == null
        ? fallback
        : Image.network(resolved, width: size, height: height, fit: BoxFit.cover, headers: AppConfig.mediaHeaders(resolved),
            errorBuilder: (_, __, ___) => fallback,
            loadingBuilder: (context, child, progress) => progress == null ? child : fallback);
    return ClipRRect(
      borderRadius: BorderRadius.circular(round ? size : 6),
      child: image,
    );
  }
}

class NameWithThumb extends StatelessWidget {
  const NameWithThumb({super.key, required this.name, required this.url, this.round = false, this.bold = true, this.extra});

  final String name;
  final String? url;
  final bool round;
  final bool bold;
  final String? extra;

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      Thumb(url: url, label: name, round: round, size: round ? 34 : 32),
      const SizedBox(width: 12),
      Flexible(
        child: Text(extra == null ? name : '$name  $extra',
            overflow: TextOverflow.ellipsis,
            style: TextStyle(fontWeight: bold ? FontWeight.w700 : FontWeight.w400)),
      ),
    ]);
  }
}

Future<bool> confirm(
  BuildContext context, {
  required String title,
  required String message,
  String confirmLabel = 'Confirm',
  bool destructive = false,
}) async {
  final result = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text(title),
      content: SizedBox(width: 380, child: Text(message)),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
        FilledButton(
          style: destructive
              ? FilledButton.styleFrom(backgroundColor: AppColors.red)
              : null,
          onPressed: () => Navigator.pop(ctx, true),
          child: Text(confirmLabel),
        ),
      ],
    ),
  );
  return result ?? false;
}

Future<bool> runAction(BuildContext context, Future<void> Function() action,
    {String? success}) async {
  try {
    await action();
    if (context.mounted && success != null) showMessage(context, success);
    return true;
  } catch (e) {
    if (context.mounted) showMessage(context, e.toString(), error: true);
    return false;
  }
}

class PageHeader extends StatelessWidget {
  const PageHeader({super.key, required this.title, this.subtitle, this.actions = const []});

  final String title;
  final String? subtitle;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 20),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title,
                    style: const TextStyle(fontSize: 30, fontWeight: FontWeight.w800)),
                if (subtitle != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text(subtitle!, style: const TextStyle(color: AppColors.muted)),
                  ),
              ],
            ),
          ),
          for (final a in actions) Padding(padding: const EdgeInsets.only(left: 12), child: a),
        ],
      ),
    );
  }
}

class Panel extends StatelessWidget {
  const Panel({super.key, required this.child, this.title, this.trailing, this.padding});

  final Widget child;
  final String? title;
  final Widget? trailing;
  final EdgeInsetsGeometry? padding;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: padding ?? const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            if (title != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 14),
                child: Row(children: [
                  Expanded(
                    child: Text(title!,
                        style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                  ),
                  if (trailing != null) trailing!,
                ]),
              ),
            child,
          ],
        ),
      ),
    );
  }
}

class StatusChip extends StatelessWidget {
  const StatusChip(this.label, {super.key, this.color});

  final String label;
  final Color? color;

  static Color colorFor(String status) {
    switch (status.toLowerCase()) {
      case 'active':
      case 'resolved':
      case 'easy':
      case 'admin':
        return AppColors.green;
      case 'reported':
      case 'pending':
      case 'medium':
      case 'locked':
        return AppColors.amber;
      case 'removed':
      case 'dismissed':
      case 'hard':
      case 'inactive':
        return AppColors.red;
      default:
        return AppColors.muted;
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = color ?? colorFor(label);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: c.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(label, style: TextStyle(color: c, fontSize: 12, fontWeight: FontWeight.w700)),
    );
  }
}

class SearchBox extends StatefulWidget {
  const SearchBox({super.key, required this.hint, required this.onChanged, this.width = 320});

  final String hint;
  final ValueChanged<String> onChanged;
  final double width;

  @override
  State<SearchBox> createState() => _SearchBoxState();
}

class _SearchBoxState extends State<SearchBox> {
  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: widget.width,
      child: TextField(
        decoration: InputDecoration(
          hintText: widget.hint,
          prefixIcon: const Icon(Icons.search, size: 20, color: AppColors.muted),
        ),
        onChanged: (v) {
          _debounce?.cancel();
          _debounce = Timer(const Duration(milliseconds: 350), () => widget.onChanged(v.trim()));
        },
      ),
    );
  }
}

class FilterDropdown<T> extends StatelessWidget {
  const FilterDropdown({
    super.key,
    required this.label,
    required this.value,
    required this.options,
    required this.onChanged,
  });

  final String label;
  final T? value;
  final Map<T, String> options;
  final ValueChanged<T?> onChanged;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 190,
      child: DropdownButtonFormField<T?>(
        initialValue: value,
        isExpanded: true,
        decoration: InputDecoration(labelText: label),
        items: [
          DropdownMenuItem<T?>(value: null, child: const Text('All')),
          for (final e in options.entries)
            DropdownMenuItem<T?>(value: e.key, child: Text(e.value)),
        ],
        onChanged: onChanged,
      ),
    );
  }
}

class PagerBar extends StatelessWidget {
  const PagerBar({
    super.key,
    required this.page,
    required this.totalPages,
    required this.totalCount,
    required this.onPage,
  });

  final int page;
  final int totalPages;
  final int totalCount;
  final ValueChanged<int> onPage;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 14),
      child: Row(
        children: [
          Text('$totalCount results', style: const TextStyle(color: AppColors.muted)),
          const Spacer(),
          IconButton(
            tooltip: 'Previous page',
            onPressed: page > 1 ? () => onPage(page - 1) : null,
            icon: const Icon(Icons.chevron_left),
          ),
          Text('$page / ${totalPages < 1 ? 1 : totalPages}'),
          IconButton(
            tooltip: 'Next page',
            onPressed: page < totalPages ? () => onPage(page + 1) : null,
            icon: const Icon(Icons.chevron_right),
          ),
        ],
      ),
    );
  }
}

class ErrorView extends StatelessWidget {
  const ErrorView({super.key, required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off, size: 40, color: AppColors.muted),
          const SizedBox(height: 12),
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
        ],
      ),
    );
  }
}

class DataColumnSpec {
  const DataColumnSpec(this.label, {this.flex = 1, this.width});

  final String label;
  final int flex;
  final double? width;
}

class DataGrid extends StatelessWidget {
  const DataGrid({
    super.key,
    required this.columns,
    required this.rows,
    this.onRowTap,
    this.emptyText = 'Nothing to show.',
  });

  final List<DataColumnSpec> columns;
  final List<List<Widget>> rows;
  final void Function(int index)? onRowTap;
  final String emptyText;

  Widget _cell(DataColumnSpec c, Widget child) => c.width != null
      ? SizedBox(width: c.width, child: child)
      : Expanded(flex: c.flex, child: child);

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Container(
            color: AppColors.surfaceHigh,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(children: [
              for (final c in columns)
                _cell(
                  c,
                  Text(c.label.toUpperCase(),
                      style: const TextStyle(
                          fontSize: 11, letterSpacing: 1, color: AppColors.muted, fontWeight: FontWeight.w700)),
                ),
            ]),
          ),
          if (rows.isEmpty)
            Padding(
              padding: const EdgeInsets.all(32),
              child: Text(emptyText, style: const TextStyle(color: AppColors.muted)),
            ),
          for (var i = 0; i < rows.length; i++)
            InkWell(
              onTap: onRowTap == null ? null : () => onRowTap!(i),
              child: Container(
                decoration: const BoxDecoration(
                    border: Border(top: BorderSide(color: AppColors.border))),
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                child: Row(children: [
                  for (var j = 0; j < columns.length; j++)
                    _cell(columns[j], Align(alignment: Alignment.centerLeft, child: rows[i][j])),
                ]),
              ),
            ),
        ],
      ),
    );
  }
}

class AsyncData<T> extends StatefulWidget {
  const AsyncData({super.key, required this.load, required this.builder});

  final Future<T> Function() load;
  final Widget Function(BuildContext context, T data, Future<void> Function() reload) builder;

  @override
  State<AsyncData<T>> createState() => _AsyncDataState<T>();
}

class _AsyncDataState<T> extends State<AsyncData<T>> {
  T? _data;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _fetch();
  }

  Future<void> _fetch() async {
    if (!mounted) return;
    setState(() {
      _loading = _data == null;
      _error = null;
    });
    try {
      final data = await widget.load();
      if (mounted) setState(() => _data = data);
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null && _data == null) {
      return ErrorView(message: _error.toString(), onRetry: _fetch);
    }
    return widget.builder(context, _data as T, _fetch);
  }
}

String formatDate(DateTime d) =>
    '${d.day.toString().padLeft(2, '0')}.${d.month.toString().padLeft(2, '0')}.${d.year}';

String formatDateTime(DateTime d) =>
    '${formatDate(d)} ${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';

String timeAgo(DateTime d) {
  final diff = DateTime.now().difference(d);
  if (diff.inMinutes < 1) return 'just now';
  if (diff.inMinutes < 60) return '${diff.inMinutes} min ago';
  if (diff.inHours < 24) return '${diff.inHours} h ago';
  if (diff.inDays < 30) return '${diff.inDays} d ago';
  return formatDate(d);
}
