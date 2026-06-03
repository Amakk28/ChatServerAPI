<script setup lang="ts">
import { ref } from 'vue'
import viteLogo from '../assets/vite.svg'
import heroImg from '../assets/hero.png'
import vueLogo from '../assets/vue.svg'

// @ts-expect-error
import LoginForm from '../components/LoginForm.vue'
</script>

<template>

    <LoginForm />

  <section id="spacer"></section>
</template>
