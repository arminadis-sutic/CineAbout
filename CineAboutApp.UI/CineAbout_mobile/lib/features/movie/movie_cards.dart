import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/models.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class PosterCard extends StatelessWidget {
  const PosterCard({super.key, required this.movie, this.width = 130});

  final MovieCard movie;
  final double width;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => context.push('/movie/${movie.id}'),
      child: SizedBox(
        width: width,
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Stack(children: [
            Poster(url: movie.posterUrl, title: movie.title, width: width, height: width * 1.5),
            Positioned(left: 6, top: 6, child: RatingBadge(movie.rating)),
          ]),
          const SizedBox(height: 6),
          Text(movie.title,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
          Text('${movie.year}${movie.genres.isEmpty ? '' : ' · ${movie.genres.first}'}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: AppColors.muted, fontSize: 12)),
        ]),
      ),
    );
  }
}

class PosterRow extends StatelessWidget {
  const PosterRow({super.key, required this.movies});

  final List<MovieCard> movies;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 250,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: movies.length,
        separatorBuilder: (_, __) => const SizedBox(width: 12),
        itemBuilder: (_, i) => PosterCard(movie: movies[i]),
      ),
    );
  }
}

class MovieTile extends StatelessWidget {
  const MovieTile({super.key, required this.movie, this.trailing, this.subtitle});

  final MovieCard movie;
  final Widget? trailing;
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push('/movie/${movie.id}'),
        child: Padding(
          padding: const EdgeInsets.all(10),
          child: Row(children: [
            Poster(url: movie.posterUrl, title: movie.title, width: 60, height: 90),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(movie.title,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
                const SizedBox(height: 4),
                Text(
                    '${movie.isSeries ? 'Series' : 'Film'} · ${movie.year}'
                    '${movie.genres.isEmpty ? '' : ' · ${movie.genres.take(2).join(', ')}'}',
                    style: const TextStyle(color: AppColors.muted, fontSize: 12)),
                const SizedBox(height: 6),
                Row(children: [
                  const Icon(Icons.star, size: 14, color: AppColors.gold),
                  const SizedBox(width: 4),
                  Text(movie.rating <= 0 ? 'No ratings' : movie.rating.toStringAsFixed(1),
                      style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
                ]),
                if (subtitle != null) ...[
                  const SizedBox(height: 4),
                  Text(subtitle!, style: const TextStyle(color: AppColors.red, fontSize: 12)),
                ],
              ]),
            ),
            if (trailing != null) trailing!,
          ]),
        ),
      ),
    );
  }
}
