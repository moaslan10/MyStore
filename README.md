# 🛒 MyStore — Full-Stack E-Commerce Platform

A modern full-stack e-commerce platform built with **React, ASP.NET Core 8 and SQLite**, featuring authentication, product management, shopping cart, checkout, orders, wishlist, reviews and an admin dashboard.

---

## 🚀 Project Overview

**MyStore** is a complete e-commerce application designed to demonstrate a real-world full-stack architecture.

The project includes separate frontend and backend applications connected through a REST API with JWT authentication and role-based authorization.

---

## ✨ Features

### 👤 Authentication & Security

- JWT authentication
- Customer and Admin roles
- Secure password hashing
- Refresh token support
- Refresh token rotation
- Logout and token revocation
- Protected API endpoints
- Role-based authorization
- Forgot password workflow

---

### 🛍️ Customer Features

- Browse products
- Product categories
- Product search
- Product details
- Shopping cart
- Wishlist
- Checkout
- Cash on Delivery
- Credit/Debit Card demo checkout
- Order history
- Order details
- User profile
- Change password
- Product reviews
- Product ratings

---

### 📦 Product Management

Admin users can:

- Add products
- Edit products
- Delete products
- Manage product information
- Manage product categories
- Track product stock
- Search products

---

### 📋 Order Management

The admin dashboard provides:

- Order list
- Order details
- Customer information
- Order status management
- Order tracking
- Stock deduction after successful orders

---

### 📊 Admin Dashboard

The administration area includes:

- Dashboard statistics
- Product management
- Category management
- Order management
- Customer management
- Inventory overview

---

## 🛠️ Tech Stack

### Frontend

- ⚛️ React
- ⚡ Vite
- 🟨 JavaScript
- 🌐 HTML5
- 🎨 CSS3

### Backend

- 🔷 ASP.NET Core 8
- 💜 C#
- 🗄️ Entity Framework Core
- 🔐 JWT Authentication
- 🔄 REST API

### Database

- 🗃️ SQLite

### Mobile

- 📱 Capacitor
- 🤖 Android Studio
- 🍎 iOS support

---

## 🏗️ Architecture

```text
MyStore
│
├── frontend
│   ├── React
│   ├── Vite
│   ├── Components
│   ├── Pages
│   └── API Integration
│
├── backend
│   └── MyStore API
│       ├── Controllers
│       ├── Models
│       ├── Services
│       ├── Data
│       └── Authentication
│
└── Database
    └── SQLite