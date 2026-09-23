import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { clearCart } from '../services/cartService';

const AdminLayout = ({ children }) => {
    const { logout } = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        clearCart();
        navigate('/');
    };

    return (
        <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', backgroundColor: 'var(--bg-color)' }}>
            {/* Admin Header */}
            <header style={{
                backgroundColor: 'var(--primary-color)',
                color: 'white',
                padding: '1rem 0',
                position: 'sticky',
                top: 0,
                zIndex: 100,
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
            }}>
                <div className="container" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem', flexWrap: 'wrap' }}>
                        <Link to="/admin" style={{ fontSize: '1.25rem', fontWeight: 'bold', color: 'white', textDecoration: 'none', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <span style={{ backgroundColor: 'rgba(255,255,255,0.2)', padding: '0.25rem 0.5rem', borderRadius: '4px', fontSize: '0.875rem' }}>Admin</span>
                            Workspace
                        </Link>
                        
                        <nav style={{ display: 'flex', gap: '1rem' }}>
                            <Link to="/admin" style={{ color: 'rgba(255,255,255,0.9)', textDecoration: 'none', fontWeight: '500' }}>Dashboard</Link>
                            <Link to="/admin/orders" style={{ color: 'rgba(255,255,255,0.9)', textDecoration: 'none', fontWeight: '500' }}>Orders</Link>
                            <Link to="/admin/products" style={{ color: 'rgba(255,255,255,0.9)', textDecoration: 'none', fontWeight: '500' }}>Products</Link>
                            <Link to="/admin/inventory" style={{ color: 'rgba(255,255,255,0.9)', textDecoration: 'none', fontWeight: '500' }}>Inventory</Link>
                            <Link to="/admin/categories" style={{ color: 'rgba(255,255,255,0.9)', textDecoration: 'none', fontWeight: '500' }}>Categories</Link>
                        </nav>
                    </div>

                    <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                        <Link to="/" style={{ color: 'rgba(255,255,255,0.9)', textDecoration: 'none', fontSize: '0.875rem' }}>&larr; Storefront</Link>
                        <button 
                            onClick={handleLogout} 
                            style={{ 
                                background: 'transparent', 
                                border: '1px solid rgba(255,255,255,0.5)', 
                                color: 'white', 
                                padding: '0.25rem 0.75rem', 
                                borderRadius: '4px',
                                cursor: 'pointer'
                            }}
                        >
                            Logout
                        </button>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <main style={{ flex: '1', padding: '2rem 0' }}>
                {children}
            </main>
        </div>
    );
};

export default AdminLayout;
