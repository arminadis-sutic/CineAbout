import 'package:flutter/material.dart';

import 'config.dart';
import 'models.dart';
import 'theme.dart';

void showMessage(BuildContext context, String message, {bool error = false}) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(
      backgroundColor: error ? const Color(0xFF7A1C22) : const Color(0xFF1E6B3A),
      duration: const Duration(seconds: 3),
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

bool validateForm(BuildContext context, GlobalKey<FormState> form,
    {String message = 'Please fix the highlighted fields.'}) {
  if (form.currentState?.validate() ?? false) return true;
  showMessage(context, message, error: true);
  return false;
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

Future<bool> confirm(BuildContext context,
    {required String title, required String message, String confirmLabel = 'Confirm'}) async {
  final result = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text(title),
      content: Text(message),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
        TextButton(
          onPressed: () => Navigator.pop(ctx, true),
          child: Text(confirmLabel, style: const TextStyle(color: AppColors.red)),
        ),
      ],
    ),
  );
  return result ?? false;
}

String formatDate(DateTime d) =>
    '${d.day.toString().padLeft(2, '0')}.${d.month.toString().padLeft(2, '0')}.${d.year}';

String timeAgo(DateTime d) {
  final diff = DateTime.now().difference(d.isUtc ? d.toLocal() : d);
  if (diff.inMinutes < 1) return 'just now';
  if (diff.inMinutes < 60) return '${diff.inMinutes} min ago';
  if (diff.inHours < 24) return '${diff.inHours} h ago';
  if (diff.inDays < 30) return '${diff.inDays} d ago';
  return formatDate(d);
}

class Poster extends StatelessWidget {
  const Poster({super.key, required this.url, this.title, this.width, this.height, this.radius = 10});

  final String? url;

  final String? title;
  final double? width, height, radius;

  @override
  Widget build(BuildContext context) {
    final resolved = AppConfig.mediaUrl(url);
    final fallback = Container(
      width: width,
      height: height,
      padding: const EdgeInsets.all(8),
      alignment: Alignment.center,
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF3A1114), Color(0xFF1B1B1B)],
        ),
      ),
      child: title == null || title!.isEmpty
          ? const Icon(Icons.movie_outlined, color: AppColors.muted)
          : Text(
              title!,
              textAlign: TextAlign.center,
              maxLines: 4,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: Colors.white70,
                fontWeight: FontWeight.w800,
                fontSize: (width ?? 100) < 80 ? 10 : 14,
              ),
            ),
    );
    return ClipRRect(
      borderRadius: BorderRadius.circular(radius ?? 0),
      child: resolved == null
          ? fallback
          : Image.network(
              resolved,
              width: width,
              height: height,
              fit: BoxFit.cover,
              errorBuilder: (_, __, ___) => fallback,
              loadingBuilder: (context, child, progress) =>
                  progress == null ? child : fallback,
            ),
    );
  }
}

class Avatar extends StatelessWidget {
  const Avatar({super.key, required this.username, this.url, this.radius = 20});

  final String username;
  final String? url;
  final double radius;

  @override
  Widget build(BuildContext context) {
    final resolved = AppConfig.mediaUrl(url);
    return CircleAvatar(
      radius: radius,
      backgroundColor: AppColors.surfaceHigh,
      backgroundImage: resolved == null ? null : NetworkImage(resolved, headers: AppConfig.mediaHeaders(resolved)),
      onBackgroundImageError: resolved == null ? null : (_, __) {},
      child: resolved == null
          ? Text(username.isEmpty ? '?' : username[0].toUpperCase(),
              style: TextStyle(fontSize: radius * 0.8, fontWeight: FontWeight.w700))
          : null,
    );
  }
}

class RatingBadge extends StatelessWidget {
  const RatingBadge(this.rating, {super.key, this.count});

  final double rating;
  final int? count;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: Colors.black.withValues(alpha: 0.6),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        const Icon(Icons.star, size: 14, color: AppColors.gold),
        const SizedBox(width: 4),
        Text(rating <= 0 ? '—' : rating.toStringAsFixed(1),
            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
        if (count != null) ...[
          const SizedBox(width: 4),
          Text('($count)', style: const TextStyle(color: AppColors.muted, fontSize: 12)),
        ],
      ]),
    );
  }
}

class SectionTitle extends StatelessWidget {
  const SectionTitle(this.title, {super.key, this.action, this.onAction});

  final String title;
  final String? action;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 20, 16, 10),
      child: Row(children: [
        Container(width: 4, height: 18, color: AppColors.red),
        const SizedBox(width: 8),
        Expanded(
          child: Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
        ),
        if (action != null)
          GestureDetector(
            onTap: onAction,
            child: Text(action!, style: const TextStyle(color: AppColors.red, fontSize: 12)),
          ),
      ]),
    );
  }
}

class Chip2 extends StatelessWidget {
  const Chip2(this.label, {super.key, this.color});

  final String label;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final c = color ?? AppColors.muted;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: c.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(label.toUpperCase(),
          style: TextStyle(color: c, fontSize: 10, fontWeight: FontWeight.w800, letterSpacing: 0.5)),
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
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          const Icon(Icons.cloud_off, size: 40, color: AppColors.muted),
          const SizedBox(height: 12),
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(
            style: OutlinedButton.styleFrom(minimumSize: const Size(140, 44)),
            onPressed: onRetry,
            child: const Text('Try again'),
          ),
        ]),
      ),
    );
  }
}

class EmptyView extends StatelessWidget {
  const EmptyView(this.message, {super.key, this.icon = Icons.inbox_outlined});

  final String message;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Icon(icon, size: 44, color: AppColors.muted),
          const SizedBox(height: 12),
          Text(message, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.muted)),
        ]),
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

class PagedListController {
  _PagedListState? _state;

  Future<void> reload() async => _state?._reset();

  Future<void> refreshSilently() async => _state?._silentRefresh();

  void removeWhere(bool Function(dynamic item) test) => _state?._removeWhere(test);

  void replaceWhere(bool Function(dynamic item) test, dynamic replacement) => _state?._replaceWhere(test, replacement);
}

class PagedList<T> extends StatefulWidget {
  const PagedList({
    super.key,
    required this.fetch,
    required this.itemBuilder,
    this.controller,
    this.emptyText = 'Nothing here yet.',
    this.padding = const EdgeInsets.fromLTRB(16, 8, 16, 24),
    this.header,
    this.separator = 12,
  });

  final Future<Paged<T>> Function(int page) fetch;
  final Widget Function(BuildContext context, T item) itemBuilder;
  final PagedListController? controller;
  final String emptyText;
  final EdgeInsets padding;
  final Widget? header;
  final double separator;

  @override
  State<PagedList<T>> createState() => _PagedListState<T>();
}

class _PagedListState<T> extends State<PagedList<T>> {
  final _scroll = ScrollController();
  final List<T> _items = [];
  int _page = 0;
  bool _hasNext = true;
  bool _loading = false;
  Object? _error;

  int _removed = 0;

  int? _pageSize;

  @override
  void initState() {
    super.initState();
    widget.controller?._state = this;
    _scroll.addListener(() {
      if (_scroll.position.pixels > _scroll.position.maxScrollExtent - 300) _loadMore();
    });
    _loadMore();
  }

  @override
  void dispose() {
    if (widget.controller?._state == this) widget.controller?._state = null;
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _reset() async {
    _items.clear();
    _page = 0;
    _removed = 0;
    _pageSize = null;
    _hasNext = true;
    _error = null;
    await _loadMore();
  }

  void _removeWhere(bool Function(dynamic item) test) {
    if (!mounted) return;
    final before = _items.length;
    setState(() {
      _items.removeWhere(test);
      _removed += before - _items.length;
    });
    if (_items.isEmpty && _hasNext && !_loading) _reset();
  }

  void _replaceWhere(bool Function(dynamic item) test, dynamic replacement) {
    if (!mounted) return;
    final hit = <int>[for (var i = 0; i < _items.length; i++) if (test(_items[i])) i];
    if (hit.isEmpty) return;
    setState(() {
      for (final i in hit) {
        _items[i] = replacement as T;
      }
    });
  }

  Future<void> _silentRefresh() async {
    if (_loading) return;
    try {
      final result = await widget.fetch(1);
      if (!mounted) return;
      setState(() {
        _items
          ..clear()
          ..addAll(result.items);
        _page = result.currentPage;
        _hasNext = result.hasNext;
        _removed = 0;
        if (result.hasNext && result.items.isNotEmpty) _pageSize = result.items.length;
      });
    } catch (_) {
    }
  }

  Future<void> _loadMore() async {
    if (_loading || !_hasNext) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final pageSize = _pageSize;
      final aligned = _removed > 0 && pageSize != null && pageSize > 0;
      final page = aligned ? _items.length ~/ pageSize + 1 : _page + 1;
      final skip = aligned ? _items.length % pageSize : 0;
      final removedAtStart = _removed;

      var result = await widget.fetch(page);
      final fresh = result.items.skip(skip).toList();
      if (skip > 0 && result.hasNext) {
        result = await widget.fetch(page + 1);
        fresh.addAll(result.items);
      }
      if (!mounted) return;
      setState(() {
        _page = result.currentPage;
        _hasNext = result.hasNext;
        _items.addAll(fresh);
        _removed = _removed > removedAtStart ? _removed - removedAtStart : 0;
        if (_pageSize == null && result.hasNext && result.items.isNotEmpty) _pageSize = result.items.length;
      });
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_items.isEmpty && _loading) return const Center(child: CircularProgressIndicator());
    if (_items.isEmpty && _error != null) {
      return ErrorView(message: _error.toString(), onRetry: _reset);
    }
    if (_items.isEmpty) {
      return RefreshIndicator(
        onRefresh: _reset,
        child: ListView(children: [
          if (widget.header != null) widget.header!,
          SizedBox(height: 280, child: EmptyView(widget.emptyText)),
        ]),
      );
    }
    final headerCount = widget.header == null ? 0 : 1;
    return RefreshIndicator(
      onRefresh: _reset,
      child: ListView.separated(
        controller: _scroll,
        padding: widget.padding,
        physics: const AlwaysScrollableScrollPhysics(),
        itemCount: _items.length + headerCount + 1,
        separatorBuilder: (_, __) => SizedBox(height: widget.separator),
        itemBuilder: (context, i) {
          if (i < headerCount) return widget.header!;
          final index = i - headerCount;
          if (index < _items.length) return widget.itemBuilder(context, _items[index]);
          if (_error != null) {
            return TextButton(onPressed: _loadMore, child: const Text('Failed to load. Tap to retry.'));
          }
          return _hasNext
              ? const Padding(
                  padding: EdgeInsets.all(16),
                  child: Center(child: CircularProgressIndicator(strokeWidth: 2)))
              : const SizedBox(height: 8);
        },
      ),
    );
  }
}
