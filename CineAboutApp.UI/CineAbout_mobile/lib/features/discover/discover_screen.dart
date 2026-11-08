import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class DiscoverScreen extends StatefulWidget {
  const DiscoverScreen({super.key});

  @override
  State<DiscoverScreen> createState() => _DiscoverScreenState();
}

class _DiscoverScreenState extends State<DiscoverScreen> {
  final List<DiscoverCard> _cards = [];
  bool _loading = true;
  Object? _error;
  double _drag = 0;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final cards = await context.read<Repository>().discover(count: 10);
      if (mounted) setState(() => _cards..clear()..addAll(cards));
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _swipe(bool like) async {
    if (_cards.isEmpty || _busy) return;
    final card = _cards.first;
    final repo = context.read<Repository>();
    setState(() => _busy = true);
    final ok = await runAction(
      context,
      () => repo.swipe(card.movie.id, like),
      success: like ? 'Added to Interested.' : null,
    );
    if (!mounted) return;
    setState(() {
      _busy = false;
      _drag = 0;
      if (ok) _cards.removeAt(0);
    });
    if (ok && _cards.isEmpty) _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text.rich(
          TextSpan(children: [
            TextSpan(text: 'DIS', style: TextStyle(color: AppColors.red)),
            TextSpan(text: 'COVER'),
          ]),
          style: TextStyle(fontWeight: FontWeight.w900, letterSpacing: 1.5),
        ),
      ),
      body: _body(),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return ErrorView(message: _error.toString(), onRetry: _load);
    if (_cards.isEmpty) {
      return Center(
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          const EmptyView('You have already rated, swiped or saved every title in the catalogue.\nNew suggestions appear when new titles are added.',
              icon: Icons.explore_outlined),
          OutlinedButton(
            style: OutlinedButton.styleFrom(minimumSize: const Size(160, 44)),
            onPressed: _load,
            child: const Text('Refresh'),
          ),
        ]),
      );
    }
    final card = _cards.first;
    final m = card.movie;
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      child: Column(children: [
        Chip2('${_cards.length} suggestions', color: AppColors.red),
        const SizedBox(height: 12),
        Expanded(
          child: GestureDetector(
            onHorizontalDragUpdate: (d) => setState(() => _drag += d.delta.dx),
            onHorizontalDragEnd: (_) {
              if (_drag > 110) {
                _swipe(true);
              } else if (_drag < -110) {
                _swipe(false);
              } else {
                setState(() => _drag = 0);
              }
            },
            onTap: () => context.push('/movie/${m.id}'),
            child: Transform.translate(
              offset: Offset(_drag, 0),
              child: Transform.rotate(
                angle: _drag / 1200,
                child: Stack(fit: StackFit.expand, children: [
                  Poster(url: m.posterUrl, title: m.title, radius: 20),
                  DecoratedBox(
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(20),
                      gradient: const LinearGradient(
                        begin: Alignment.topCenter,
                        end: Alignment.bottomCenter,
                        colors: [Colors.transparent, Colors.black87],
                        stops: [0.45, 1],
                      ),
                    ),
                  ),
                  Positioned(left: 16, right: 16, bottom: 16, child: _CardInfo(card: card)),
                  if (_drag.abs() > 30)
                    Positioned(
                      top: 24,
                      left: _drag > 0 ? 24 : null,
                      right: _drag < 0 ? 24 : null,
                      child: Chip2(_drag > 0 ? 'Interested' : 'Not for me',
                          color: _drag > 0 ? AppColors.gold : AppColors.red),
                    ),
                ]),
              ),
            ),
          ),
        ),
        const SizedBox(height: 16),
        Row(mainAxisAlignment: MainAxisAlignment.spaceEvenly, children: [
          _RoundAction(
            icon: Icons.close,
            color: AppColors.red,
            label: 'NOT FOR ME',
            onTap: _busy ? null : () => _swipe(false),
          ),
          _RoundAction(
            icon: Icons.favorite,
            color: AppColors.gold,
            label: 'INTERESTED',
            onTap: _busy ? null : () => _swipe(true),
          ),
        ]),
      ]),
    );
  }
}

class _CardInfo extends StatelessWidget {
  const _CardInfo({required this.card});

  final DiscoverCard card;

  @override
  Widget build(BuildContext context) {
    final m = card.movie;
    return Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: [
      Row(children: [
        for (final g in m.genres.take(2)) ...[Chip2(g, color: Colors.white), const SizedBox(width: 6)],
        const Spacer(),
        RatingBadge(m.rating),
      ]),
      const SizedBox(height: 10),
      Text(m.title, style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w900)),
      Text('${m.year}', style: const TextStyle(color: AppColors.muted)),
      if (card.reason.isNotEmpty) ...[
        const SizedBox(height: 10),
        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          const Icon(Icons.auto_awesome, size: 14, color: AppColors.gold),
          const SizedBox(width: 6),
          Expanded(
            child: Text(card.reason, style: const TextStyle(fontSize: 13, color: Colors.white70)),
          ),
        ]),
      ],
    ]);
  }
}

class _RoundAction extends StatelessWidget {
  const _RoundAction({required this.icon, required this.color, required this.label, this.onTap});

  final IconData icon;
  final Color color;
  final String label;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Column(mainAxisSize: MainAxisSize.min, children: [
      Material(
        color: AppColors.surfaceHigh,
        borderRadius: BorderRadius.circular(18),
        child: InkWell(
          borderRadius: BorderRadius.circular(18),
          onTap: onTap,
          child: Container(
            width: 64,
            height: 64,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(18),
              border: Border.all(color: color.withValues(alpha: 0.5)),
            ),
            child: Icon(icon, color: color, size: 28),
          ),
        ),
      ),
      const SizedBox(height: 6),
      Text(label, style: const TextStyle(fontSize: 10, color: AppColors.muted, letterSpacing: 1)),
    ]);
  }
}
