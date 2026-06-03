<script>
export default {
  data() {
    return {
      username: '',
      password: ''
    }
  },
  methods: {
    handleLogin() {
        console.log('Logging in with', this.username, this.password)
    }
  }
}
</script>

<template>
  <div class="login-container">
    <h1>Login</h1>
    <form @submit.prevent="handleLogin">
      <div class="form-group">
        <label for="username">Username:</label>
        <input type="text" id="username" v-model="username" required />
        </div>
        <div class="form-group">
        <label for="password">Password:</label>
        <input type="password" id="password" v-model="password" required />
        </div>
        <button type="submit">Login</button>
    </form>
    </div>
</template>