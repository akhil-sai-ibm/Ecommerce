import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import MainLayout from './layouts/MainLayout';
import AdminLayout from './layouts/AdminLayout';
import { ProtectedRoute, AdminRoute } from './components/ProtectedRoutes';
import { SignalRProvider } from './context/SignalRContext';
import OrderNotificationToast from './components/OrderNotificationToast';

import Home from './pages/Home';
import Login from './pages/Login';
import Register from './pages/Register';
import HealthCheck from './pages/HealthCheck';
import Products from './pages/Products';
import ProductDetails from './pages/ProductDetails';
import Cart from './pages/Cart';
import Checkout from './pages/Checkout';
import Orders from './pages/Orders';
import OrderDetails from './pages/OrderDetails';
import Profile from './pages/Profile';

import AdminDashboard from './pages/admin/AdminDashboard';
import AdminProducts from './pages/admin/AdminProducts';
import AdminProductForm from './pages/admin/AdminProductForm';
import AdminBulkUpload from './pages/admin/AdminBulkUpload';
import AdminInventory from './pages/admin/AdminInventory';
import AdminCategories from './pages/admin/AdminCategories';
import AdminCategoryForm from './pages/admin/AdminCategoryForm';
import AdminOrders from './pages/admin/AdminOrders';
import AdminOrderDetails from './pages/admin/AdminOrderDetails';

function App() {
  return (
    <AuthProvider>
      <Router>
        <SignalRProvider>
          <OrderNotificationToast />
          <Routes>
          {/* Admin Routes with AdminLayout */}
          <Route path="/admin/*" element={
            <AdminRoute>
              <AdminLayout>
                <Routes>
                  <Route path="/" element={<AdminDashboard />} />
                  <Route path="/products" element={<AdminProducts />} />
                  <Route path="/products/new" element={<AdminProductForm />} />
                  <Route path="/products/bulk-upload" element={<AdminBulkUpload />} />
                  <Route path="/products/:id/edit" element={<AdminProductForm />} />
                  <Route path="/inventory" element={<AdminInventory />} />
                  <Route path="/categories" element={<AdminCategories />} />
                  <Route path="/categories/new" element={<AdminCategoryForm />} />
                  <Route path="/categories/:id/edit" element={<AdminCategoryForm />} />
                  <Route path="/orders" element={<AdminOrders />} />
                  <Route path="/orders/:id" element={<AdminOrderDetails />} />
                  <Route path="*" element={<Navigate to="/admin" replace />} />
                </Routes>
              </AdminLayout>
            </AdminRoute>
          } />

          {/* Customer/Public Routes with MainLayout */}
          <Route path="/*" element={
            <MainLayout>
              <Routes>
                {/* Public Routes */}
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />
                <Route path="/health" element={<HealthCheck />} />
                
                {/* Protected Routes */}
                <Route path="/" element={
                  <ProtectedRoute>
                    <Home />
                  </ProtectedRoute>
                } />
                <Route path="/products" element={
                  <ProtectedRoute>
                    <Products />
                  </ProtectedRoute>
                } />
                <Route path="/products/:id" element={
                  <ProtectedRoute>
                    <ProductDetails />
                  </ProtectedRoute>
                } />
                <Route path="/cart" element={
                  <ProtectedRoute>
                    <Cart />
                  </ProtectedRoute>
                } />
                <Route path="/checkout" element={
                  <ProtectedRoute>
                    <Checkout />
                  </ProtectedRoute>
                } />
                <Route path="/orders" element={
                  <ProtectedRoute>
                    <Orders />
                  </ProtectedRoute>
                } />
                <Route path="/orders/:id" element={
                  <ProtectedRoute>
                    <OrderDetails />
                  </ProtectedRoute>
                } />
                <Route path="/profile" element={
                  <ProtectedRoute>
                    <Profile />
                  </ProtectedRoute>
                } />
                
                {/* Fallback */}
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </MainLayout>
          } />
        </Routes>
        </SignalRProvider>
      </Router>
    </AuthProvider>
  );
}

export default App;
