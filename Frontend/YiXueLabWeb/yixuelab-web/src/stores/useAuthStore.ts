import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { authApi } from '../api/yixueApi';
import { UserRole, type UserProfileDto } from '../types/yixue';

export const useAuthStore = defineStore('yi-auth', () => {
  const token = ref<string | null>(localStorage.getItem('yixuelab_jwt_token'));
  const user = ref<UserProfileDto | null>(
    localStorage.getItem('yixuelab_user_profile')
      ? JSON.parse(localStorage.getItem('yixuelab_user_profile')!)
      : null
  );

  // 适老化大字号开关
  const isElderFontLarge = ref<boolean>(
    localStorage.getItem('yixuelab_elder_font') === 'true'
  );

  const isAuthenticated = computed(() => !!token.value && !!user.value);
  const isAdmin = computed(() => user.value?.role === UserRole.Admin);
  const canPublish = computed(
    () => user.value?.role === UserRole.Admin || user.value?.role === UserRole.Publisher
  );

  async function login(username: string, password: string) {
    const res = await authApi.login(username, password);
    setSession(res.accessToken, res.user);
  }

  async function register(
    username: string,
    password: string,
    wechatNickname: string,
    inviteCode: string
  ) {
    const res = await authApi.register(username, password, wechatNickname, inviteCode);
    setSession(res.accessToken, res.user);
  }

  function setSession(newToken: string, profile: UserProfileDto) {
    token.value = newToken;
    user.value = profile;
    localStorage.setItem('yixuelab_jwt_token', newToken);
    localStorage.setItem('yixuelab_user_profile', JSON.stringify(profile));
  }

  function logout() {
    token.value = null;
    user.value = null;
    localStorage.removeItem('yixuelab_jwt_token');
    localStorage.removeItem('yixuelab_user_profile');
  }

  function toggleElderFont() {
    isElderFontLarge.value = !isElderFontLarge.value;
    localStorage.setItem('yixuelab_elder_font', String(isElderFontLarge.value));
  }

  return {
    token,
    user,
    isElderFontLarge,
    isAuthenticated,
    isAdmin,
    canPublish,
    login,
    register,
    logout,
    toggleElderFont
  };
});