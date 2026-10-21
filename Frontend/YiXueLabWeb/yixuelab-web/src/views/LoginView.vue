<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/useAuthStore';

const router = useRouter();
const authStore = useAuthStore();

const isRegisterMode = ref(false);
const username = ref('');
const password = ref('');
const wechatNickname = ref('');
const inviteCode = ref('');
const errorMsg = ref('');
const loading = ref(false);

async function handleSubmit() {
  errorMsg.value = '';
  loading.value = true;
  try {
    if (isRegisterMode.value) {
      await authStore.register(username.value, password.value, wechatNickname.value, inviteCode.value);
    } else {
      await authStore.login(username.value, password.value);
    }
    router.push('/');
  } catch (err: any) {
    errorMsg.value = err.message || '操作失败，请核对输入信息';
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="min-h-screen bg-paper-bg flex flex-col justify-center px-5 py-8 max-w-md mx-auto">
    <div class="bg-paper-card border border-paper-border rounded-2xl p-6 shadow-lg space-y-5">
      <div class="text-center space-y-1.5">
        <div class="w-12 h-12 rounded-xl bg-cinnabar text-white font-serif font-bold text-2xl mx-auto flex items-center justify-center shadow">
          易
        </div>
        <h1 class="font-serif font-bold text-xl text-ink">易学练功房 · 内部通行验证</h1>
        <p class="text-xs text-ink-muted">仅限持有群主邀请码的内部易友访问</p>
      </div>

      <!-- 模式切换 -->
      <div class="grid grid-cols-2 bg-paper-sub p-1 rounded-xl border border-paper-border">
        <button
          type="button"
          @click="isRegisterMode = false"
          class="py-2 rounded-lg text-sm font-serif font-bold transition"
          :class="!isRegisterMode ? 'bg-cinnabar text-white shadow-sm' : 'text-ink-light'"
        >
          老群友登录
        </button>
        <button
          type="button"
          @click="isRegisterMode = true"
          class="py-2 rounded-lg text-sm font-serif font-bold transition"
          :class="isRegisterMode ? 'bg-cinnabar text-white shadow-sm' : 'text-ink-light'"
        >
          凭邀请码注册
        </button>
      </div>

      <div v-if="errorMsg" class="p-3 rounded-xl bg-cinnabar-light border border-cinnabar/40 text-cinnabar text-xs font-medium">
        ⚠️ {{ errorMsg }}
      </div>

      <form @submit.prevent="handleSubmit" class="space-y-3.5">
        <div v-if="isRegisterMode">
          <label class="block text-xs font-bold text-cinnabar mb-1">内部群友邀请码 (一码一人)</label>
          <input
            v-model="inviteCode"
            type="text"
            required
            placeholder="例如：YX-8F3A9C"
            class="w-full rounded-xl bg-paper-bg border border-paper-border p-3 text-sm font-mono uppercase focus:outline-none focus:border-cinnabar"
          />
        </div>

        <div v-if="isRegisterMode">
          <label class="block text-xs font-bold text-ink mb-1">您的微信群昵称 (用于群内实名核对)</label>
          <input
            v-model="wechatNickname"
            type="text"
            required
            placeholder="例如：王师兄 / 陈长老"
            class="w-full rounded-xl bg-paper-bg border border-paper-border p-3 text-sm focus:outline-none focus:border-cinnabar"
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-ink mb-1">登录账号</label>
          <input
            v-model="username"
            type="text"
            required
            placeholder="请输入账号 (初始管理员为 admin)"
            class="w-full rounded-xl bg-paper-bg border border-paper-border p-3 text-sm focus:outline-none focus:border-cinnabar"
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-ink mb-1">登录密码</label>
          <input
            v-model="password"
            type="password"
            required
            placeholder="请输入密码"
            class="w-full rounded-xl bg-paper-bg border border-paper-border p-3 text-sm focus:outline-none focus:border-cinnabar"
          />
        </div>

        <button
          type="submit"
          :disabled="loading"
          class="w-full py-3.5 rounded-xl bg-cinnabar hover:bg-cinnabar-dark text-white font-serif font-bold text-base shadow-md transition disabled:opacity-50"
        >
          {{ loading ? '正在验证通行证...' : (isRegisterMode ? '核销邀请码并进入练功房' : '进入练功房') }}
        </button>
      </form>
    </div>
  </div>
</template>