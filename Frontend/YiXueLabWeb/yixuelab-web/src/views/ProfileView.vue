<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { authApi } from '../api/yixueApi';
import { useAuthStore } from '../stores/useAuthStore';

const authStore = useAuthStore();
const router = useRouter();
const latestInviteCode = ref('');
const copied = ref(false);

async function handleGenerateCode() {
  latestInviteCode.value = await authApi.generateInviteCode();
  copied.value = false;
}

function copyCode() {
  if (!latestInviteCode.value) return;
  navigator.clipboard.writeText(
    `【易学练功房】您的内部注册邀请码为：${latestInviteCode.value}（一码一人，注册即核销）`
  );
  copied.value = true;
}

function handleLogout() {
  authStore.logout();
  router.push('/login');
}
</script>

<template>
  <div class="space-y-4">
    <div class="bg-paper-card border border-paper-border rounded-xl p-5 flex items-center justify-between">
      <div>
        <h2 class="font-serif font-bold text-lg text-ink">{{ authStore.user?.wechatNickname }}</h2>
        <p class="text-xs text-ink-muted mt-0.5">账号：{{ authStore.user?.username }}</p>
      </div>
      <button @click="handleLogout" class="px-3 py-1.5 rounded-lg border border-paper-border text-xs text-ink-light">
        退出登录
      </button>
    </div>

    <!-- 管理员专属：邀请码生成器 -->
    <div v-if="authStore.isAdmin" class="bg-paper-card rounded-xl p-4 space-y-3 border-2 border-gold/40">
      <h3 class="font-serif font-bold text-base text-ink">🔑 内部群友邀请码签发台</h3>
      <div v-if="latestInviteCode" class="bg-paper-bg rounded-xl p-4 border border-paper-border flex items-center justify-between">
        <span class="font-mono font-bold text-2xl text-cinnabar tracking-widest">{{ latestInviteCode }}</span>
        <button @click="copyCode" class="px-3 py-1.5 rounded-lg bg-gold-light border border-gold/40 text-gold text-xs font-bold">
          {{ copied ? '✓ 已复制' : '复制发到微信群' }}
        </button>
      </div>
      <button
        @click="handleGenerateCode"
        class="w-full py-2.5 rounded-xl bg-ink text-white text-sm font-medium"
      >
        + 生成一枚新邀请码
      </button>
    </div>
  </div>
</template>