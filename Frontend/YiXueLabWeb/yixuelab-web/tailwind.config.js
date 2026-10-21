/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        paper: {
          bg: '#F7F4ED',      // 宣纸暖白底色
          card: '#FFFDF9',    // 卡片象牙白
          sub: '#EFECE4',     // 次级底色
          border: '#DFD7C8',  // 宣纸边框色
        },
        ink: {
          DEFAULT: '#2B2825', // 浓墨主字色
          light: '#605A52',   // 淡墨次字色
          muted: '#948C80',   // 辅助灰褐
        },
        cinnabar: {
          DEFAULT: '#9E2A2B', // 朱砂红（主强调色）
          light: '#FDF2F2',
          dark: '#7A1E1F',
        },
        jade: {
          DEFAULT: '#2A6F4E', // 翡翠绿（已揭晓/吉）
          light: '#EBF5F0',
        },
        gold: {
          DEFAULT: '#B58328', // 泥金黄
          light: '#FDF8EE',
        },
        wuxing: {
          wood: '#2E6F40',    // 木 - 苍翠
          fire: '#C83E37',    // 火 - 丹红
          earth: '#8C6239',   // 土 - 赭石
          metal: '#B58328',   // 金 - 赤金
          water: '#1F567D',   // 水 - 靛蓝
        }
      },
      fontFamily: {
        serif: ['"Noto Serif SC"', 'STSong', 'SimSun', 'serif'],
        sans: ['"Noto Sans SC"', '-apple-system', 'BlinkMacSystemFont', 'sans-serif'],
      }
    },
  },
  plugins: [],
}