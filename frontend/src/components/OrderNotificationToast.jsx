import React, { useEffect, useState, useRef } from 'react';
import { useSignalR } from '../context/SignalRContext';
import { Link } from 'react-router-dom';

const OrderNotificationToast = () => {
    const { connection } = useSignalR();
    const [notification, setNotification] = useState(null);
    const timeoutRef = useRef(null);

    useEffect(() => {
        if (!connection) return;

        const handleUpdate = (payload) => {
            // Clear any existing timeout before setting a new one
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }

            setNotification(payload);
            
            // Auto dismiss after 5 seconds
            timeoutRef.current = setTimeout(() => {
                setNotification(null);
            }, 5000);
        };

        connection.on("ReceiveOrderStatusUpdate", handleUpdate);

        return () => {
            connection.off("ReceiveOrderStatusUpdate", handleUpdate);
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }
        };
    }, [connection]);

    if (!notification) return null;

    return (
        <div style={{
            position: 'fixed',
            bottom: '20px',
            right: '20px',
            backgroundColor: 'var(--primary-color)',
            color: 'white',
            padding: '1rem',
            borderRadius: '4px',
            boxShadow: '0 4px 6px rgba(0,0,0,0.1)',
            zIndex: 1000,
            maxWidth: '300px',
            borderLeft: '4px solid #4ade80' // A subtle green left border
        }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                <strong>Order Update</strong>
                <button 
                    onClick={() => setNotification(null)}
                    style={{ background: 'none', border: 'none', color: 'white', cursor: 'pointer', fontSize: '1.2rem', lineHeight: 1 }}
                >
                    &times;
                </button>
            </div>
            <p style={{ margin: '0 0 0.5rem 0', fontSize: '0.875rem' }}>
                {notification.message}
            </p>
            <div style={{ fontSize: '0.75rem', textAlign: 'right' }}>
                <Link to={`/orders/${notification.orderId}`} style={{ color: 'white', textDecoration: 'underline' }}>
                    View Order
                </Link>
            </div>
        </div>
    );
};

export default OrderNotificationToast;
