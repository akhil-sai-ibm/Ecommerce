import React, { useState, useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getCart } from '../services/cartService';
import { checkout } from '../services/orderService';

const Checkout = () => {
    const [cart, setCart] = useState(null);
    const [loading, setLoading] = useState(true);
    const [checkoutLoading, setCheckoutLoading] = useState(false);
    const [error, setError] = useState(null);
    const [successOrder, setSuccessOrder] = useState(null);
    const [shippingDetails, setShippingDetails] = useState({
        shippingAddressLine1: '',
        shippingAddressLine2: '',
        shippingCity: '',
        shippingState: '',
        shippingZipCode: '',
        shippingCountry: '',
        shippingPhoneNumber: ''
    });
    
    // Idempotency key must be generated once per checkout session
    const idempotencyKeyRef = useRef(crypto.randomUUID());
    const navigate = useNavigate();

    useEffect(() => {
        const fetchCart = async () => {
            try {
                const data = await getCart();
                setCart(data);
            } catch (err) {
                if (err.response && err.response.status === 404) {
                    setCart({ items: [], total: 0 });
                } else {
                    setError('Failed to load cart for checkout.');
                }
            } finally {
                setLoading(false);
            }
        };

        fetchCart();
    }, []);

    const handleCheckout = async (e) => {
        if (e) e.preventDefault();
        
        try {
            setCheckoutLoading(true);
            setError(null);
            
            const requestDto = {
                shippingAddressLine1: shippingDetails.shippingAddressLine1,
                shippingAddressLine2: shippingDetails.shippingAddressLine2,
                shippingCity: shippingDetails.shippingCity,
                shippingState: shippingDetails.shippingState,
                shippingPostalCode: shippingDetails.shippingZipCode,
                shippingCountry: shippingDetails.shippingCountry,
                shippingPhoneNumber: shippingDetails.shippingPhoneNumber
            };
            
            const order = await checkout(idempotencyKeyRef.current, requestDto);
            setSuccessOrder(order);
        } catch (err) {
            if (err.response && err.response.status === 409) {
                setError(err.response.data?.error || 'A conflict occurred during checkout. Please try again later.');
            } else {
                const errorMsg = err.response?.data?.message || err.response?.data?.error || 'Failed to complete checkout. Please try again.';
                setError(errorMsg);
            }
        } finally {
            setCheckoutLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="container">
                <div className="loading-state">Loading checkout...</div>
            </div>
        );
    }

    if (successOrder) {
        return (
            <div className="container" style={{ padding: '2rem 1rem' }}>
                <div className="card text-center" style={{ padding: '4rem 2rem', maxWidth: '600px', margin: '0 auto' }}>
                    <div style={{ 
                        width: '64px', height: '64px', backgroundColor: 'var(--success-color)', 
                        color: 'white', borderRadius: '50%', display: 'flex', alignItems: 'center', 
                        justifyContent: 'center', margin: '0 auto 1.5rem', fontSize: '2rem'
                    }}>
                        ✓
                    </div>
                    <h1 style={{ marginBottom: '1rem' }}>Order Created Successfully!</h1>
                    <p style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }}>
                        Your order has been placed. Order ID:
                    </p>
                    <p style={{ fontWeight: 'bold', fontSize: '1.125rem', marginBottom: '2rem', wordBreak: 'break-all' }}>
                        {successOrder.id}
                    </p>
                    <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center', flexWrap: 'wrap' }}>
                        <Link to={`/orders/${successOrder.id}`} className="btn btn-primary btn-large">
                            View Order
                        </Link>
                        <Link to="/products" className="btn btn-secondary btn-large">
                            Continue Shopping
                        </Link>
                    </div>
                </div>
            </div>
        );
    }

    if (!cart || !cart.items || cart.items.length === 0) {
        return (
            <div className="container">
                <div className="card text-center" style={{ padding: '4rem 2rem', marginTop: '2rem' }}>
                    <h2 style={{ marginBottom: '1rem' }}>Your cart is empty.</h2>
                    <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem' }}>
                        You cannot proceed to checkout with an empty cart.
                    </p>
                    <Link to="/products" className="btn btn-primary btn-large">
                        Continue Shopping
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="container" style={{ padding: '2rem 1rem' }}>
            <button className="btn btn-secondary back-button" style={{ marginBottom: '2rem' }} onClick={() => navigate('/cart')}>
                &larr; Back to Cart
            </button>

            <h1 style={{ marginBottom: '2rem' }}>Checkout</h1>

            {error && <div className="alert alert-error">{error}</div>}

            <div className="checkout-layout" style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
                <div className="checkout-details" style={{ flex: '1 1 60%', minWidth: '300px' }}>
                    <div className="card">
                        <h2 style={{ marginBottom: '1.5rem', paddingBottom: '1rem', borderBottom: '1px solid var(--border-color)' }}>
                            Items to Purchase
                        </h2>
                        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                            {cart.items.map(item => (
                                <li key={item.id} style={{ padding: '1rem 0', borderBottom: '1px solid var(--border-color)', display: 'flex', justifyContent: 'space-between' }}>
                                    <div>
                                        <div style={{ fontWeight: '500' }}>{item.productName}</div>
                                        <div style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>Qty: {item.quantity} &times; ${item.unitPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</div>
                                    </div>
                                    <div style={{ fontWeight: '500' }}>
                                        ${(item.quantity * item.unitPrice).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>
                    
                    <div className="card" style={{ marginTop: '2rem' }}>
                        <h2 style={{ marginBottom: '1.5rem', paddingBottom: '1rem', borderBottom: '1px solid var(--border-color)' }}>
                            Shipping Details
                        </h2>
                        <form id="checkout-form" onSubmit={handleCheckout}>
                            <div className="form-group">
                                <label htmlFor="shippingAddressLine1">Address Line 1*</label>
                                <input type="text" id="shippingAddressLine1" className="input" required value={shippingDetails.shippingAddressLine1} onChange={(e) => setShippingDetails({...shippingDetails, shippingAddressLine1: e.target.value})} />
                            </div>
                            <div className="form-group">
                                <label htmlFor="shippingAddressLine2">Address Line 2</label>
                                <input type="text" id="shippingAddressLine2" className="input" value={shippingDetails.shippingAddressLine2} onChange={(e) => setShippingDetails({...shippingDetails, shippingAddressLine2: e.target.value})} />
                            </div>
                            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                                <div className="form-group" style={{ flex: '1 1 45%' }}>
                                    <label htmlFor="shippingCity">City*</label>
                                    <input type="text" id="shippingCity" className="input" required value={shippingDetails.shippingCity} onChange={(e) => setShippingDetails({...shippingDetails, shippingCity: e.target.value})} />
                                </div>
                                <div className="form-group" style={{ flex: '1 1 45%' }}>
                                    <label htmlFor="shippingState">State*</label>
                                    <input type="text" id="shippingState" className="input" required value={shippingDetails.shippingState} onChange={(e) => setShippingDetails({...shippingDetails, shippingState: e.target.value})} />
                                </div>
                            </div>
                            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                                <div className="form-group" style={{ flex: '1 1 45%' }}>
                                    <label htmlFor="shippingZipCode">Zip Code*</label>
                                    <input type="text" id="shippingZipCode" className="input" required value={shippingDetails.shippingZipCode} onChange={(e) => setShippingDetails({...shippingDetails, shippingZipCode: e.target.value})} />
                                </div>
                                <div className="form-group" style={{ flex: '1 1 45%' }}>
                                    <label htmlFor="shippingCountry">Country*</label>
                                    <input type="text" id="shippingCountry" className="input" required value={shippingDetails.shippingCountry} onChange={(e) => setShippingDetails({...shippingDetails, shippingCountry: e.target.value})} />
                                </div>
                            </div>
                            <div className="form-group">
                                <label htmlFor="shippingPhoneNumber">Phone Number*</label>
                                <input type="tel" id="shippingPhoneNumber" className="input" required value={shippingDetails.shippingPhoneNumber} onChange={(e) => setShippingDetails({...shippingDetails, shippingPhoneNumber: e.target.value})} />
                            </div>
                        </form>
                    </div>
                </div>

                <div className="checkout-summary" style={{ flex: '1 1 30%', minWidth: '300px' }}>
                    <div className="card">
                        <h2 style={{ marginBottom: '1.5rem', paddingBottom: '1rem', borderBottom: '1px solid var(--border-color)' }}>
                            Summary
                        </h2>
                        
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2rem', fontSize: '1.5rem', fontWeight: 'bold' }}>
                            <span>Total</span>
                            <span>${cart.total.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                        </div>
                        
                        <button 
                            type="submit"
                            form="checkout-form"
                            className="btn btn-primary w-full btn-large" 
                            style={{ padding: '0.75rem' }}
                            disabled={checkoutLoading}
                        >
                            {checkoutLoading ? 'Processing...' : 'Place Order'}
                        </button>
                        <p style={{ marginTop: '1rem', fontSize: '0.875rem', color: 'var(--text-secondary)', textAlign: 'center' }}>
                            Payment processing will be handled in a future step.
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Checkout;
