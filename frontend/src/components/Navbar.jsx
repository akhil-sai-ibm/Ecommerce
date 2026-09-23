import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { clearCart } from '../services/cartService';

const Navbar = () => {
    const { isAuthenticated, isAdmin, logout } = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        clearCart();
        navigate('/');
    };

    return (
        <nav style={{ 
            backgroundColor: 'var(--bg-color)', 
            borderBottom: '1px solid var(--border-color)',
            padding: '1rem 0',
            position: 'sticky',
            top: 0,
            zIndex: 100
        }}>
            <div className="container" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Link to="/" style={{ fontSize: '1.5rem', fontWeight: 'bold', color: 'var(--primary-color)', textDecoration: 'none' }}>
                    ECommerce
                </Link>

                <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                    {isAuthenticated ? (
                        <>
                            {isAdmin && (
                                <Link to="/admin" style={{ color: 'var(--text-color)', textDecoration: 'none', fontWeight: '500', marginRight: '0.5rem', display: 'flex', alignItems: 'center' }}>
                                    <span style={{ backgroundColor: 'var(--primary-color)', color: 'white', padding: '0.2rem 0.5rem', borderRadius: '4px', fontSize: '0.75rem', marginRight: '0.25rem' }}>Admin</span>
                                    Dashboard
                                </Link>
                            )}
                            <Link to="/products" style={{ color: 'var(--text-color)', textDecoration: 'none' }}>Products</Link>
                            <Link to="/orders" style={{ color: 'var(--text-color)', textDecoration: 'none' }}>My Orders</Link>
                            <Link to="/profile" style={{ color: 'var(--text-color)', textDecoration: 'none' }}>Profile</Link>
                            <Link to="/cart" style={{ color: 'var(--text-color)', textDecoration: 'none' }}>Cart</Link>
                            <button onClick={handleLogout} className="btn btn-secondary" style={{ padding: '0.5rem 1rem' }}>
                                Logout
                            </button>
                        </>
                    ) : (
                        <>
                            <Link to="/login" className="btn btn-secondary" style={{ padding: '0.5rem 1rem', marginRight: '0.5rem' }}>Login</Link>
                            <Link to="/register" className="btn btn-primary" style={{ padding: '0.5rem 1rem' }}>Register</Link>
                        </>
                    )}
                </div>
            </div>
        </nav>
    );
};

export default Navbar;
