# 🎮 Custom Login and Cloud Game Data System

A complete solution for user authentication and cloud-based game data management, built with **Unity3D**, **Next.js**, **Node.js**, and **MongoDB**.

---

## 🔗 Repositories Used

To fully test and run the system, use all **three** repositories together:

1. 🎮 `Unity3d_project_for_login_saveGame`  
   → Unity3D client for game login, register, and save game data

2. 🌐 `Nodejs_server_for_Unity3dGame_Login_saveData`  
   → Node.js + MongoDB backend server

3. 🖥️ `webapp_for_Unity3dGame_nextjs-`  
   → Next.js frontend web app for login, account creation, and password reset using the same backend

---

## 🛠️ Features

### ✅ Unity3D (Frontend)

- Custom login and registration UI
- Save and fetch player data from the cloud
- Upload and retrieve profile pictures
- JWT-based secure authentication
- Error handling and auto login with token

### ⚙️ Node.js + Express (Backend)

- Argon2 for strong password hashing
- JWT for authentication with auto-refresh
- Email verification and forgot password functionality using Nodemailer
- Multer for profile picture upload (multipart form support)
- IP blocking after multiple failed login attempts
- Express-validator for input sanitization
- Modular routes and controller structure for scalability

### 💾 MongoDB + Mongoose

- Cloud-hosted MongoDB via MongoDB Atlas
- Mongoose schemas for user and game data
- Secure and structured data management

### 🌐 Next.js Web App

- Clean and responsive login and registration forms
- Forgot password and password reset flow with JWT email verification
- Connects to the same backend as Unity3D app
- Built using App Router and Tailwind CSS
- Reusable form components and token validation logic

---

## 🌐 Project Structure

```
.
├── Unity3d_project_for_login_saveGame
│   └── Unity 3D Game Client
├── Nodejs_server_for_Unity3dGame_Login_saveData
│   └── Express.js Backend API
├── webapp_for_Unity3dGame_nextjs-
    └── Next.js Web Frontend (Login, Create Account, Reset Password)
```

---

## 🚀 How to Run

### 1. Clone and Start Backend Server

```bash
git clone https://github.com/deezero7/Nodejs_server_for_Unity3dGame_Login_saveData
cd Nodejs_server_for_Unity3dGame_Login_saveData
npm install
npm start
```

- Configure your MongoDB URI and JWT secret in `.env`

### 2. Run the Unity Project

- Open `Unity3d_project_for_login_saveGame` in Unity Editor
- Set backend API URL in Unity scripts
- Play and test login/register + save/load player data

### 3. Run the Next.js Web App

```bash
git clone https://github.com/deezero7/webapp_for_Unity3dGame_nextjs-
cd webapp_for_Unity3dGame_nextjs-
npm install
npm run dev
```

- Set the backend API URL in your `.env.local` file

---

## ✅ Highlights

- Custom login and registration system
- Cloud-based save/load of player game data
- Email verification and password reset via JWT tokens
- Argon2 password hashing
- Secure image upload using Multer
- IP blocking on repeated failed logins
- Modular and scalable backend architecture
- Frontend apps built with both Unity3D and Next.js accessing the same backend

---
