import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getOrders } from '../services/orderService';
import { useSignalR } from '../context/SignalRContext';

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

export const getOrderStatusLabel = (status) => {
    switch (status) {
        case 0: return 'Pending';
        case 1: return 'Confirmed';
        case 2: return 'Processing';
        case 3: return 'Shipped';
        case 4: return 'Delivered';
        case 5: return 'Cancelled';
        default: return 'Unknown';
    }
};

export const getOrderStatusBadgeClass = (status) => {
    switch (status) {
        case 0: return 'badge badge-neutral'; // Pending
        case 1: return 'badge badge-neutral'; // Confirmed
        case 2: return 'badge badge-neutral'; // Processing
        case 3: return 'badge badge-success'; // Shipped
        case 4: return 'badge badge-success'; // Delivered
        case 5: return 'badge badge-error';   // Cancelled
        default: return 'badge';
    }
};

const Orders = () => {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const { connection } = useSignalR();

    useEffect(() => {
        if (!connection) return;

        const handleUpdate = (payload) => {
            const parsedStatus = parseStatus(payload.newStatus);
            setOrders(prevOrders => 
                prevOrders.map(o => 
                    o.id === payload.orderId 
                        ? { ...o, status: parsedStatus } 
                        : o
                )
            );
        };

        connection.on("ReceiveOrderStatusUpdate", handleUpdate);

        return () => {
            connection.off("ReceiveOrderStatusUpdate", handleUpdate);
        };
    }, [connection]);

    useEffect(() => {
        const fetchOrders = async () => {
            try {
                const data = await getOrders();
                // Sort by OrderDate descending
                const sortedOrders = data.sort((a, b) => new Date(b.orderDate) - new Date(a.orderDate));
                setOrders(sortedOrders);
            } catch (err) {
                setError('Failed to load orders.');
            } finally {
                setLoading(false);
            }
        };

        fetchOrders();
    }, []);

    if (loading) {
        return (
            <div className="container">
                <div className="loading-state">Loading your orders...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="container" style={{ padding: '2rem 1rem' }}>
                <div className="alert alert-error">{error}</div>
            </div>
        );
    }

    if (orders.length === 0) {
        return (
            <div className="container" style={{ padding: '2rem 1rem' }}>
                <h1 style={{ marginBottom: '2rem' }}>My Orders</h1>
                <div className="card text-center" style={{ padding: '4rem 2rem' }}>
                    <h2 style={{ marginBottom: '1rem' }}>You have no orders.</h2>
                    <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem' }}>
                        When you place an order, it will appear here.
                    </p>
                    <Link to="/products" className="btn btn-primary btn-large">
                        Start Shopping
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="container" style={{ padding: '2rem 1rem' }}>
            <h1 style={{ marginBottom: '2rem' }}>My Orders</h1>
            
            <div className="orders-list" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                {orders.map(order => (
                    <div key={order.id} className="card" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
                        <div>
                            <div style={{ fontWeight: 'bold', marginBottom: '0.25rem' }}>Order ID: {order.id}</div>
                            <div style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: '0.25rem' }}>
                                Date: {new Date(order.orderDate).toLocaleString()}
                            </div>
                            <div style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: '0.5rem' }}>
                                Items: {order.items?.length || 0}
                            </div>
                            <span className={getOrderStatusBadgeClass(order.status)}>
                                {getOrderStatusLabel(order.status)}
                            </span>
                        </div>
                        <div style={{ textAlign: 'right' }}>
                            <div style={{ fontWeight: 'bold', fontSize: '1.25rem', marginBottom: '1rem' }}>
                                ${order.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </div>
                            <Link to={`/orders/${order.id}`} className="btn btn-secondary">
                                View Details
                            </Link>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default Orders;
