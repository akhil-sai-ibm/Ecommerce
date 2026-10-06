import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getCart, updateCartItem, removeCartItem, clearCart } from '../services/cartService';

const Cart = () => {
    const [cart, setCart] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionLoading, setActionLoading] = useState(false);
    const navigate = useNavigate();

    const fetchCart = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await getCart();
            setCart(data);
        } catch (err) {
            if (err.response && err.response.status === 404) {
                // Cart might be completely missing before the first item is added, treat as empty
                setCart({ items: [], total: 0 });
            } else {
                setError('Failed to load cart. Please try again.');
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchCart();
    }, []);

    const handleUpdateQuantity = async (cartItemId, currentQuantity, change) => {
        const newQuantity = currentQuantity + change;
        if (newQuantity < 1) return;

        try {
            setActionLoading(true);
            setError(null);
            await updateCartItem(cartItemId, newQuantity);
            await fetchCart(); // Refresh cart to get the authoritative total from backend
        } catch (err) {
            const errorMsg = err.response?.data?.message || err.response?.data?.error || 'Failed to update quantity. Ensure enough stock is available.';
            setError(errorMsg);
        } finally {
            setActionLoading(false);
        }
    };

    const handleRemoveItem = async (cartItemId) => {
        try {
            setActionLoading(true);
            setError(null);
            await removeCartItem(cartItemId);
            await fetchCart();
        } catch (err) {
            setError('Failed to remove item.');
        } finally {
            setActionLoading(false);
        }
    };

    const handleClearCart = async () => {
        if (!window.confirm('Are you sure you want to clear your cart?')) return;
        try {
            setActionLoading(true);
            setError(null);
            await clearCart();
            await fetchCart();
        } catch (err) {
            setError('Failed to clear cart.');
        } finally {
            setActionLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="container">
                <div className="loading-state">Loading cart...</div>
            </div>
        );
    }

    if (!cart || !cart.items || cart.items.length === 0) {
        return (
            <div className="container">
                <div className="card text-center" style={{ padding: '4rem 2rem', marginTop: '2rem' }}>
                    <h2 style={{ marginBottom: '1rem' }}>Your cart is empty.</h2>
                    <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem' }}>
                        Looks like you haven't added any items to your cart yet.
                    </p>
                    <Link to="/products" className="btn btn-primary btn-large">
                        Continue Shopping
                    </Link>
                </div>
            </div>
        );
    }

    const baseUrl = import.meta.env.VITE_API_BASE_URL?.replace('/api', '') || 'https://localhost:52549';
    const resolveImageUrl = (url) => url ? (url.startsWith('http') ? url : `${baseUrl}${url}`) : null;

    const cartTotal = cart.items.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);

    return (
        <div className="container" style={{ padding: '2rem 1rem' }}>
            <h1 style={{ marginBottom: '2rem' }}>Your Shopping Cart</h1>

            {error && <div className="alert alert-error">{error}</div>}

            <div className="cart-layout" style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
                <div className="cart-items" style={{ flex: '1 1 60%', minWidth: '300px' }}>
                    <div className="card" style={{ padding: '0' }}>
                        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                            {cart.items.map(item => (
                                <li key={item.id} style={{ padding: '1.5rem', borderBottom: '1px solid var(--border-color)', display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: '1rem' }}>
                                    <div style={{ display: 'flex', gap: '1rem', flex: '1 1 200px' }}>
                                        <div style={{ width: '80px', height: '80px', flexShrink: 0, backgroundColor: '#f3f4f6', borderRadius: '4px', overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                                            {(item.productImageUrl || item.imageUrl) ? (
                                                <img src={resolveImageUrl(item.productImageUrl || item.imageUrl)} alt={item.productName} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                                            ) : (
                                                <span style={{ fontSize: '0.75rem', color: '#9ca3af' }}>No Image</span>
                                            )}
                                        </div>
                                        <div>
                                            <h3 style={{ fontSize: '1.125rem', marginBottom: '0.25rem' }}>{item.productName}</h3>
                                            <div style={{ color: 'var(--text-secondary)' }}>
                                                Unit Price: ${item.unitPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                            </div>
                                        </div>
                                    </div>
                                    
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                                        <div style={{ display: 'flex', alignItems: 'center', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)', overflow: 'hidden' }}>
                                            <button 
                                                className="btn" 
                                                style={{ padding: '0.5rem 0.75rem', backgroundColor: '#f9fafb', borderRight: '1px solid var(--border-color)', borderRadius: 0 }}
                                                onClick={() => handleUpdateQuantity(item.id, item.quantity, -1)}
                                                disabled={item.quantity <= 1 || actionLoading}
                                            >
                                                -
                                            </button>
                                            <span style={{ padding: '0.5rem 1rem', minWidth: '3rem', textAlign: 'center' }}>
                                                {item.quantity}
                                            </span>
                                            <button 
                                                className="btn" 
                                                style={{ padding: '0.5rem 0.75rem', backgroundColor: '#f9fafb', borderLeft: '1px solid var(--border-color)', borderRadius: 0 }}
                                                onClick={() => handleUpdateQuantity(item.id, item.quantity, 1)}
                                                disabled={actionLoading}
                                            >
                                                +
                                            </button>
                                        </div>
                                        <button 
                                            className="btn btn-secondary" 
                                            onClick={() => handleRemoveItem(item.id)}
                                            disabled={actionLoading}
                                            style={{ color: 'var(--error-color)', borderColor: 'var(--border-color)' }}
                                        >
                                            Remove
                                        </button>
                                    </div>
                                </li>
                            ))}
                        </ul>
                        <div style={{ padding: '1rem 1.5rem', backgroundColor: '#f9fafb', borderTop: '1px solid var(--border-color)', textAlign: 'left' }}>
                            <button 
                                className="btn btn-secondary" 
                                onClick={handleClearCart} 
                                disabled={actionLoading}
                            >
                                Clear Cart
                            </button>
                        </div>
                    </div>
                </div>

                <div className="cart-summary" style={{ flex: '1 1 30%', minWidth: '300px' }}>
                    <div className="card">
                        <h2 style={{ marginBottom: '1.5rem', paddingBottom: '1rem', borderBottom: '1px solid var(--border-color)' }}>Order Summary</h2>
                        
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', fontSize: '1.125rem' }}>
                            <span>Subtotal</span>
                            <span>${cartTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                        </div>
                        
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2rem', fontSize: '1.5rem', fontWeight: 'bold' }}>
                            <span>Total</span>
                            <span>${cartTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                        </div>
                        
                        <button 
                            className="btn btn-primary w-full btn-large" 
                            style={{ padding: '0.75rem' }}
                            onClick={() => navigate('/checkout')}
                            disabled={actionLoading}
                        >
                            Proceed to Checkout
                        </button>
                        <div style={{ marginTop: '1rem', textAlign: 'center' }}>
                            <Link to="/products" style={{ color: 'var(--text-secondary)' }}>Continue Shopping</Link>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Cart;
