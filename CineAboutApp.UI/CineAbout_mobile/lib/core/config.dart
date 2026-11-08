class AppConfig {
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5218',
  );

  static String? mediaUrl(String? path) {
    if (path == null || path.trim().isEmpty) return null;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    return '$baseUrl${path.startsWith('/') ? '' : '/'}$path';
  }

  static Map<String, String>? mediaHeaders(String? resolvedUrl) {
    final token = MediaAuth.token;
    if (token == null || resolvedUrl == null || !resolvedUrl.startsWith('$baseUrl/api/')) return null;
    return {'Authorization': 'Bearer $token'};
  }
}

class MediaAuth {
  static String? token;
}
