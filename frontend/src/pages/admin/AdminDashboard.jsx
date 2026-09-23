import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { searchProducts } from '../../services/productService';
import { useAuth } from '../../context/AuthContext';

const AdminDashboard = () => {
    const { user } = useAuth();
    const [totalProducts, setTotalProducts] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const fetchDashboardData = async () => {
            try {
                setLoading(true);
                // We request 1 item just to get the X-Total-Count header
                const res = await searchProducts({ pageSize: 1 });
                setTotalProducts(res.totalCount);
            } catch (err) {
                console.error("Failed to fetch dashboard data:", err);
                setError("Could not load dashboard statistics.");
            } finally {
                setLoading(false);
            }
        };

        fetchDashboardData();
    }, []);

    return (
        <div className="container">
            <h1 style={{ marginBottom: '0.5rem' }}>Welcome, Admin {user?.email}</h1>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem' }}>
                Manage your store catalog and settings from this workspace.
            </p>

            {error && (
                <div className="alert alert-error" style={{ marginBottom: '1.5rem' }}>
                    {error}
                </div>
            )}

            <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap' }}>
                <div className="card" style={{ flex: '1 1 300px', padding: '1.5rem' }}>
                    <h3 style={{ margin: '0 0 0.5rem 0', color: 'var(--text-secondary)', fontSize: '1rem', fontWeight: '500' }}>
                        Total Products
                    </h3>
                    <div style={{ fontSize: '2.5rem', fontWeight: 'bold', marginBottom: '1rem', color: 'var(--text-color)' }}>
                        {loading ? '...' : totalProducts !== null ? totalProducts : '-'}
                    </div>
                    <Link to="/admin/products" className="btn btn-secondary" style={{ display: 'inline-block' }}>
                        Manage Products
                    </Link>
                </div>
            </div>
        </div>
    );
};

export default AdminDashboard;
