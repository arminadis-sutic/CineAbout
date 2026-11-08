import 'package:flutter/material.dart';

import 'models.dart';
import 'widgets.dart';

class PagedTableController {
  _PagedTableState? _state;

  Future<void> reload() async => _state?._load();

  Future<void> reset() async {
    _state?._page = 1;
    await _state?._load();
  }
}

class PagedTable<T> extends StatefulWidget {
  const PagedTable({
    super.key,
    required this.controller,
    required this.fetch,
    required this.columns,
    required this.rowBuilder,
    this.onRowTap,
    this.emptyText = 'Nothing to show.',
  });

  final PagedTableController controller;
  final Future<Paged<T>> Function(int page) fetch;
  final List<DataColumnSpec> columns;
  final List<Widget> Function(T item) rowBuilder;
  final void Function(T item)? onRowTap;
  final String emptyText;

  @override
  State<PagedTable<T>> createState() => _PagedTableState<T>();
}

class _PagedTableState<T> extends State<PagedTable<T>> {
  Paged<T>? _data;
  Object? _error;
  bool _loading = true;
  int _page = 1;

  @override
  void initState() {
    super.initState();
    widget.controller._state = this;
    _load();
  }

  @override
  void dispose() {
    if (widget.controller._state == this) widget.controller._state = null;
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _data == null;
      _error = null;
    });
    try {
      var data = await widget.fetch(_page);
      if (data.items.isEmpty && _page > 1) {
        _page = data.totalPages < 1 ? 1 : data.totalPages;
        data = await widget.fetch(_page);
      }
      if (mounted) setState(() => _data = data);
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Padding(
        padding: EdgeInsets.all(48),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    final data = _data;
    if (_error != null || data == null) {
      return Padding(
        padding: const EdgeInsets.all(32),
        child: ErrorView(message: '${_error ?? 'Failed to load.'}', onRetry: _load),
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        DataGrid(
          columns: widget.columns,
          rows: [for (final item in data.items) widget.rowBuilder(item)],
          emptyText: widget.emptyText,
          onRowTap: widget.onRowTap == null
              ? null
              : (i) => widget.onRowTap!(data.items[i]),
        ),
        PagerBar(
          page: data.currentPage,
          totalPages: data.totalPages,
          totalCount: data.totalCount,
          onPage: (p) {
            _page = p;
            _load();
          },
        ),
      ],
    );
  }
}
