import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getOrderById, cancelOrder } from '../services/orderService';
import { searchProducts } from '../services/productService';
import { getPaymentByOrderId, createPayment, processPayment } from '../services/paymentService';
import { getOrderStatusLabel, getOrderStatusBadgeClass } from './Orders';
import { useSignalR } from '../context/SignalRContext';

const OrderDetails = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const [order, setOrder] = useState(null);
    const [payment, setPayment] = useState(null);
    const [productMap, setProductMap] = useState({});
    
    // UI State
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionLoading, setActionLoading] = useState(false);
    const [actionError, setActionError] = useState(null);
    
    // Payment State
    const [showPaymentForm, setShowPaymentForm] = useState(false);
    const [selectedMethod, setSelectedMethod] = useState(0); // Default: CreditCard
    const [paymentProcessing, setPaymentProcessing] = useState(false);
    const [paymentError, setPaymentError] = useState(null);
    const { connection } = useSignalR();

    useEffect(() => {
        if (!connection) return;

        const handleUpdate = (payload) => {
            if (payload.orderId === id) {
                const parseStatus = (statusStr) => {
                    switch (statusStr) {
                        case 'Pending': return 0;
                        case 'Confirmed': return 1;
                        case 'Processing': return 2;
                        case 'Shipped': return 3;
                        case 'Delivered': return 4;
                        case 'Cancelled': return 5;
                        default: return parseInt(statusStr) || 0;
                    }
                };
                setOrder(prev => prev ? { ...prev, status: parseStatus(payload.newStatus) } : prev);
            }
        };

        connection.on("ReceiveOrderStatusUpdate", handleUpdate);

        return () => {
            connection.off("ReceiveOrderStatusUpdate", handleUpdate);
        };
    }, [connection, id]);

    const paymentMethods = [
        { value: 0, label: 'Credit Card' },
        { value: 1, label: 'Debit Card' },
        { value: 2, label: 'UPI' },
        { value: 3, label: 'Net Banking' },
        { value: 4, label: 'Cash on Delivery' }
    ];

    const getPaymentStatusLabel = (status) => {
        switch (status) {
            case 0: return 'Pending';
            case 1: return 'Successful';
            case 2: return 'Failed';
            case 3: return 'Refunded';
            default: return 'Unknown';
        }
    };

    const getPaymentMethodLabel = (method) => {
        const found = paymentMethods.find(m => m.value === method);
        return found ? found.label : 'Unknown';
    };

    const fetchOrderDetails = async () => {
        try {
            setLoading(true);
            setError(null);
            
            const [orderData, productsResult, paymentData] = await Promise.all([
                getOrderById(id),
                searchProducts({ pageSize: 100 }).catch(() => ({ data: [] })),
                getPaymentByOrderId(id).catch(() => null) // May 404 if no payment
            ]);

            const map = {};
            productsResult.data.forEach(p => {
                map[p.id] = p.name;
            });

            setProductMap(map);
            setOrder(orderData);
            setPayment(paymentData);
        } catch (err) {
            if (err.response && (err.response.status === 404 || err.response.status === 403)) {
                setError('Order not found or you do not have permission to view it.');
            } else {
                setError('Failed to load order details.');
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchOrderDetails();
    }, [id]);

    const handleCancelOrder = async () => {
        if (!window.confirm('Are you sure you want to cancel this order? This action cannot be undone.')) {
            return;
        }

        try {
            setActionLoading(true);
            setActionError(null);
            await cancelOrder(id);
            await fetchOrderDetails();
        } catch (err) {
            const errorMsg = err.response?.data?.message || err.response?.data?.error || 'Failed to cancel the order. It may have already been shipped or delivered.';
            setActionError(errorMsg);
        } finally {
            setActionLoading(false);
        }
    };

    const handleProcessPayment = async (e) => {
        e.preventDefault();
        try {
            setPaymentProcessing(true);
            setPaymentError(null);

            // Step 1: Create a new payment record
            const newPayment = await createPayment({
                orderId: id,
                amount: order.totalAmount,
                method: parseInt(selectedMethod, 10)
            });

            // Step 2: Process the payment
            const processedPayment = await processPayment(newPayment.id);
            
            // Refresh order & payment state
            await fetchOrderDetails();
            setShowPaymentForm(false);
        } catch (err) {
            const errorMsg = err.response?.data?.message || err.response?.data?.error || 'Failed to process payment.';
            setPaymentError(errorMsg);
            
            // Refresh to ensure we have the latest payment status (e.g., if it failed during processing)
            await fetchOrderDetails();
        } finally {
            setPaymentProcessing(false);
        }
    };

    if (loading) {
        return (
            <div className="container">
                <div className="loading-state">Loading order details...</div>
            </div>
        );
    }

    if (error || !order) {
        return (
            <div className="container" style={{ padding: '2rem 1rem' }}>
                <div className="alert alert-error">{error || 'Order not found.'}</div>
                <button className="btn btn-secondary" onClick={() => navigate('/orders')} style={{ marginTop: '1rem' }}>
                    &larr; Back to My Orders
                </button>
            </div>
        );
    }

    const canCancel = order.status === 0 || order.status === 1 || order.status === 2;
    const canPay = (order.status === 0 || order.status === 1 || order.status === 2) && (!payment || payment.status === 2);
    
    return (
        <div className="container" style={{ padding: '2rem 1rem' }}>
            <button className="btn btn-secondary back-button" onClick={() => navigate('/orders')} style={{ marginBottom: '2rem' }}>
                &larr; Back to My Orders
            </button>

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem', marginBottom: '2rem' }}>
                <h1 style={{ margin: 0 }}>Order Details</h1>
                <span className={getOrderStatusBadgeClass(order.status)} style={{ fontSize: '1rem', padding: '0.5rem 1rem' }}>
                    {getOrderStatusLabel(order.status)}
                </span>
            </div>

            {actionError && <div className="alert alert-error" style={{ marginBottom: '1.5rem' }}>{actionError}</div>}

            <div className="order-details-layout" style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
                <div className="order-items" style={{ flex: '1 1 60%', minWidth: '300px' }}>
                    <div className="card" style={{ padding: '0' }}>
                        <h2 style={{ padding: '1.5rem', margin: 0, borderBottom: '1px solid var(--border-color)' }}>Items</h2>
                        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                            {order.items?.map(item => (
                                <li key={item.id} style={{ padding: '1.5rem', borderBottom: '1px solid var(--border-color)', display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: '1rem' }}>
                                    <div>
                                        <h3 style={{ fontSize: '1.125rem', marginBottom: '0.25rem', fontWeight: '500' }}>
                                            {productMap[item.productId] || `Product ID: ${item.productId}`}
                                        </h3>
                                        <div style={{ color: 'var(--text-secondary)' }}>
                                            Qty: {item.quantity} &times; ${item.unitPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                        </div>
                                    </div>
                                    <div style={{ fontWeight: '500', fontSize: '1.125rem' }}>
                                        ${(item.quantity * item.unitPrice).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                    </div>
                                </li>
                            ))}
                            {(!order.items || order.items.length === 0) && (
                                <li style={{ padding: '1.5rem', color: 'var(--text-secondary)' }}>No items found in this order.</li>
                            )}
                        </ul>
                    </div>
                </div>

                <div className="order-summary" style={{ flex: '1 1 30%', minWidth: '300px' }}>
                    <div className="card" style={{ marginBottom: '1.5rem' }}>
                        <h2 style={{ marginBottom: '1.5rem', paddingBottom: '1rem', borderBottom: '1px solid var(--border-color)' }}>Summary</h2>
                        
                        <div style={{ marginBottom: '1rem', color: 'var(--text-secondary)' }}>
                            <div style={{ marginBottom: '0.5rem' }}>
                                <strong>Order ID:</strong> <br/><span style={{ fontSize: '0.875rem', wordBreak: 'break-all' }}>{order.id}</span>
                            </div>
                            <div style={{ marginBottom: '0.5rem' }}>
                                <strong>Date:</strong> {new Date(order.orderDate).toLocaleString()}
                            </div>
                        </div>

                        <div style={{ marginBottom: '1rem', paddingTop: '1rem', borderTop: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                            <h3 style={{ fontSize: '1rem', marginBottom: '0.75rem', color: 'var(--text-color)' }}>Shipping Details</h3>
                            <div style={{ fontSize: '0.875rem' }}>
                                {order.shippingAddressLine1}<br/>
                                {order.shippingAddressLine2 && <>{order.shippingAddressLine2}<br/></>}
                                {order.shippingCity}, {order.shippingState} {order.shippingZipCode}<br/>
                                {order.shippingCountry}<br/>
                                Phone: {order.shippingPhoneNumber}
                            </div>
                        </div>

                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2rem', fontSize: '1.5rem', fontWeight: 'bold', paddingTop: '1rem', borderTop: '1px solid var(--border-color)' }}>
                            <span>Total</span>
                            <span>${order.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                        </div>
                        
                        {canCancel && (
                            <div style={{ paddingTop: '1rem', borderTop: '1px solid var(--border-color)' }}>
                                <button 
                                    className="btn btn-secondary w-full" 
                                    onClick={handleCancelOrder}
                                    disabled={actionLoading || paymentProcessing}
                                    style={{ color: 'var(--error-color)', borderColor: 'var(--border-color)' }}
                                >
                                    {actionLoading ? 'Cancelling...' : 'Cancel Order'}
                                </button>
                                <p style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textAlign: 'center', marginTop: '0.75rem' }}>
                                    Cancelling this order will stop processing and release the items.
                                </p>
                            </div>
                        )}
                    </div>

                    <div className="card">
                        <h2 style={{ marginBottom: '1.5rem', paddingBottom: '1rem', borderBottom: '1px solid var(--border-color)' }}>Payment</h2>
                        
                        {payment && payment.status === 1 ? (
                            <div>
                                <div className="alert alert-success" style={{ marginBottom: '1rem' }}>Payment Successful</div>
                                <div style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                                    <div style={{ marginBottom: '0.5rem' }}><strong>Status:</strong> {getPaymentStatusLabel(payment.status)}</div>
                                    <div style={{ marginBottom: '0.5rem' }}><strong>Method:</strong> {getPaymentMethodLabel(payment.method)}</div>
                                    <div style={{ marginBottom: '0.5rem' }}><strong>Transaction ID:</strong> <span style={{ wordBreak: 'break-all' }}>{payment.transactionId}</span></div>
                                    <div style={{ marginBottom: '0.5rem' }}><strong>Amount:</strong> ${payment.amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</div>
                                </div>
                            </div>
                        ) : payment && payment.status === 0 && !showPaymentForm ? (
                            <div>
                                <div className="badge badge-neutral" style={{ marginBottom: '1rem' }}>Payment Pending</div>
                                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
                                    This order has a pending payment.
                                </p>
                            </div>
                        ) : payment && payment.status === 2 && !showPaymentForm ? (
                            <div>
                                <div className="alert alert-error" style={{ marginBottom: '1rem' }}>Payment Failed</div>
                                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
                                    Your previous payment attempt failed.
                                </p>
                                {canPay && (
                                    <button 
                                        className="btn btn-primary w-full" 
                                        onClick={() => setShowPaymentForm(true)}
                                    >
                                        Retry Payment
                                    </button>
                                )}
                            </div>
                        ) : canPay && !showPaymentForm ? (
                            <div>
                                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
                                    No payment has been made for this order yet.
                                </p>
                                <button 
                                    className="btn btn-primary w-full" 
                                    onClick={() => setShowPaymentForm(true)}
                                >
                                    Pay Now
                                </button>
                            </div>
                        ) : showPaymentForm ? (
                            <form onSubmit={handleProcessPayment}>
                                {paymentError && <div className="alert alert-error" style={{ marginBottom: '1rem' }}>{paymentError}</div>}
                                
                                <div className="form-group" style={{ marginBottom: '1rem' }}>
                                    <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Amount</label>
                                    <div style={{ padding: '0.75rem', backgroundColor: 'var(--bg-color)', border: '1px solid var(--border-color)', borderRadius: '4px' }}>
                                        ${order.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                    </div>
                                    <small style={{ color: 'var(--text-secondary)', display: 'block', marginTop: '0.5rem' }}>
                                        Amount is locked to the order total.
                                    </small>
                                </div>

                                <div className="form-group" style={{ marginBottom: '1.5rem' }}>
                                    <label htmlFor="paymentMethod" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Payment Method</label>
                                    <select 
                                        id="paymentMethod"
                                        className="form-control" 
                                        value={selectedMethod}
                                        onChange={(e) => setSelectedMethod(e.target.value)}
                                        disabled={paymentProcessing}
                                        style={{ width: '100%', padding: '0.75rem', border: '1px solid var(--border-color)', borderRadius: '4px' }}
                                    >
                                        {paymentMethods.map(method => (
                                            <option key={method.value} value={method.value}>{method.label}</option>
                                        ))}
                                    </select>
                                </div>

                                <div style={{ display: 'flex', gap: '1rem' }}>
                                    <button 
                                        type="submit" 
                                        className="btn btn-primary" 
                                        style={{ flex: '1' }}
                                        disabled={paymentProcessing}
                                    >
                                        {paymentProcessing ? 'Processing...' : 'Submit Payment'}
                                    </button>
                                    <button 
                                        type="button" 
                                        className="btn btn-secondary" 
                                        onClick={() => setShowPaymentForm(false)}
                                        disabled={paymentProcessing}
                                    >
                                        Cancel
                                    </button>
                                </div>
                            </form>
                        ) : (
                            <p style={{ color: 'var(--text-secondary)' }}>Payment action unavailable.</p>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default OrderDetails;
