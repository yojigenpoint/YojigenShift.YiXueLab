import axios, { type AxiosError } from 'axios';

export interface ApiErrorResponse {
  code: number;
  message: string;
  timestampUtc: string;
}

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7043/api',
  timeout: 12000,
  headers: {
    'Content-Type': 'application/json'
  }
});

// 请求拦截器：自动注入 JWT Bearer Token
http.interceptors.request.use((config) => {
  const token = localStorage.getItem('yixuelab_jwt_token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// 响应拦截器：统一提取后端 ApiControllerBase 返回的中文错误信息
http.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiErrorResponse>) => {
    if (error.response?.status === 401) {
      // Token 过期或未登录，清理本地缓存并跳转登录页
      localStorage.removeItem('yixuelab_jwt_token');
      localStorage.removeItem('yixuelab_user_profile');
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }

    const serverMsg = error.response?.data?.message || '网络连接异常，请稍后再试';
    return Promise.reject(new Error(serverMsg));
  }
);

export default http;