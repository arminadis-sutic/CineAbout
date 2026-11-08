import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'config.dart';

class ApiException implements Exception {
  ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

class TokenStore {
  TokenStore([FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;
  static const _jwtKey = 'jwt';
  static const _refreshKey = 'refresh';

  Future<String?> get jwt async {
    final value = await _storage.read(key: _jwtKey);
    MediaAuth.token = value;
    return value;
  }

  Future<String?> get refresh => _storage.read(key: _refreshKey);

  Future<void> save(String jwt, String refresh) async {
    await _storage.write(key: _jwtKey, value: jwt);
    await _storage.write(key: _refreshKey, value: refresh);
    MediaAuth.token = jwt;
  }

  Future<void> clear() async {
    await _storage.delete(key: _jwtKey);
    await _storage.delete(key: _refreshKey);
    MediaAuth.token = null;
  }
}

class ApiClient {
  ApiClient(this.tokens, {this.onSessionExpired}) {
    _dio = Dio(BaseOptions(
      baseUrl: '${AppConfig.baseUrl}/api/',
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      contentType: Headers.jsonContentType,
    ));
    _refreshDio = Dio(BaseOptions(
      baseUrl: '${AppConfig.baseUrl}/api/',
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 20),
    ));
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await tokens.jwt;
        if (token != null && options.extra['anonymous'] != true) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      onError: (error, handler) async {
        final request = error.requestOptions;
        final canRetry = error.response?.statusCode == 401 &&
            request.extra['anonymous'] != true &&
            request.extra['retried'] != true;
        if (!canRetry) return handler.next(error);

        if (await _tryRefresh()) {
          request.extra['retried'] = true;
          try {
            return handler.resolve(await _dio.fetch<dynamic>(request));
          } on DioException catch (e) {
            return handler.next(e);
          }
        }
        await tokens.clear();
        onSessionExpired?.call();
        handler.next(error);
      },
    ));
  }

  final TokenStore tokens;
  final void Function()? onSessionExpired;
  late final Dio _dio;
  late final Dio _refreshDio;
  Future<bool>? _refreshing;

  Future<bool> _tryRefresh() => _refreshing ??= _doRefresh().whenComplete(() {
        _refreshing = null;
      });

  Future<bool> _doRefresh() async {
    final jwt = await tokens.jwt;
    final refresh = await tokens.refresh;
    if (jwt == null || refresh == null) return false;
    try {
      final res = await _refreshDio.post<Map<String, dynamic>>('User/refresh',
          data: {'jwtToken': jwt, 'refreshToken': refresh});
      final data = res.data;
      if (data == null) return false;
      await tokens.save(data['jwtToken'] as String, data['refreshToken'] as String);
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<dynamic> _run(Future<Response<dynamic>> Function() call) async {
    try {
      return (await call()).data;
    } on DioException catch (e) {
      throw _toException(e);
    }
  }

  ApiException _toException(DioException e) {
    final status = e.response?.statusCode;
    if (status == null) {
      return ApiException('Cannot reach the server. Check your connection.');
    }
    final data = e.response?.data;
    String? message;
    if (data is Map) {
      final errors = data['errors'];
      if (errors is Map && errors.isNotEmpty) {
        final first = errors.values.first;
        message = first is List && first.isNotEmpty ? first.first.toString() : first.toString();
      }
      message ??= data['message']?.toString() ?? data['detail']?.toString();
    } else if (data is String && data.isNotEmpty && data.length < 300) {
      message = data;
    }
    message ??= switch (status) {
      401 => 'Your session has expired. Please sign in again.',
      403 => 'You do not have permission to do that.',
      404 => 'Not found.',
      _ => 'Request failed ($status).',
    };
    return ApiException(message, statusCode: status);
  }

  Map<String, dynamic>? _clean(Map<String, dynamic>? query) {
    if (query == null) return null;
    return {
      for (final e in query.entries)
        if (e.value != null && e.value.toString().isNotEmpty) e.key: e.value,
    };
  }

  Future<dynamic> get(String path, {Map<String, dynamic>? query}) =>
      _run(() => _dio.get<dynamic>(path, queryParameters: _clean(query)));

  Future<dynamic> post(String path, {Object? body, bool anonymous = false}) => _run(() =>
      _dio.post<dynamic>(path, data: body, options: Options(extra: {'anonymous': anonymous})));

  Future<dynamic> put(String path, {Object? body}) =>
      _run(() => _dio.put<dynamic>(path, data: body));

  Future<dynamic> putForm(String path, Map<String, dynamic> fields,
      {String? filePath, String fileField = 'file'}) async {
    final form = FormData.fromMap(fields);
    if (filePath != null) {
      final lower = filePath.toLowerCase();
      final type = lower.endsWith('.png')
          ? 'image/png'
          : lower.endsWith('.webp')
              ? 'image/webp'
              : 'image/jpeg';
      form.files.add(MapEntry(fileField, await MultipartFile.fromFile(filePath, contentType: DioMediaType.parse(type))));
    }
    return _run(() => _dio.put<dynamic>(path,
        data: form, options: Options(contentType: Headers.multipartFormDataContentType)));
  }

  Future<dynamic> delete(String path) => _run(() => _dio.delete<dynamic>(path));
}
