import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { adminOrderService } from '../../services/adminOrderService';

const AdminOrders = () => {
    const [searchType, setSearchType] = useState('userId');
    const [searchValue, setSearchValue] = useState('');
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [hasSearched, setHasSearched] = useState(false);

    const getStatusBadge = (status) => {
        switch (status) {
            case 0: return <span className="badge badge-warning">Pending</span>;
            case 1: return <span className="badge badge-info">Confirmed</span>;
            case 2: return <span className="badge badge-primary">Processing</span>;
            case 3: return <span className="badge badge-accent">Shipped</span>;
            case 4: return <span className="badge badge-success">Delivered</span>;
            case 5: return <span className="badge badge-neutral">Cancelled</span>;
            default: return <span className="badge badge-neutral">Unknown</span>;
        }
    };

    const handleSearch = async (e) => {
        e.preventDefault();
        if (!searchValue.trim()) return;

        setLoading(true);
        setError(null);
        setOrders([]);
        setHasSearched(true);

        try {
            if (searchType === 'userId') {
                const results = await adminOrderService.getOrdersByUser(searchValue.trim());
                setOrders(results || []);
            } else if (searchType === 'orderId') {
                const result = await adminOrderService.getOrderById(searchValue.trim());
                if (result) {
                    setOrders([result]);
                }
            }
        } catch (err) {
            console.error("Search failed:", err);
            if (err.response && err.response.status === 404) {
                // Return empty list if not found
                setOrders([]);
            } else {
                setError("An error occurred while searching for orders.");
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="container">
            <div className="d-flex align-items-center justify-content-between mb-4">
                <h1 className="h3 mb-0">Order Management</h1>
            </div>

            <div className="card mb-4">
                <div className="card-body">
                    <form onSubmit={handleSearch} className="d-flex align-items-end gap-3 flex-wrap">
                        <div className="form-group flex-grow-1" style={{ maxWidth: '200px' }}>
                            <label className="form-label" htmlFor="searchType">Search By</label>
                            <select 
                                id="searchType"
                                className="form-control"
                                value={searchType}
                                onChange={(e) => {
                                    setSearchType(e.target.value);
                                    setSearchValue('');
                                    setOrders([]);
                                    setHasSearched(false);
                                    setError(null);
                                }}
                            >
                                <option value="userId">User ID</option>
                                <option value="orderId">Order ID</option>
                            </select>
                        </div>
                        <div className="form-group flex-grow-1" style={{ maxWidth: '400px' }}>
                            <label className="form-label" htmlFor="searchValue">
                                {searchType === 'userId' ? 'Enter User ID' : 'Enter Order ID'}
                            </label>
                            <input 
                                id="searchValue"
                                type="text"
                                className="form-control"
                                placeholder="e.g. 123e4567-e89b-12d3-a456-426614174000"
                                value={searchValue}
                                onChange={(e) => setSearchValue(e.target.value)}
                                required
                            />
                        </div>
                        <div className="form-group">
                            <button type="submit" className="btn btn-primary" disabled={loading || !searchValue.trim()}>
                                {loading ? 'Searching...' : 'Search'}
                            </button>
                        </div>
                    </form>
                </div>
            </div>

            {error && (
                <div className="alert alert-error mb-4">
                    {error}
                </div>
            )}

            {!loading && hasSearched && orders.length === 0 && !error && (
                <div className="card text-center p-5">
                    <p className="text-muted mb-0">No orders found for the given ID.</p>
                </div>
            )}

            {orders.length > 0 && (
                <div className="card">
                    <div className="table-responsive">
                        <table className="table table-striped table-hover mb-0">
                            <thead>
                                <tr>
                                    <th>Order ID</th>
                                    <th>User ID</th>
                                    <th>Items</th>
                                    <th>Total Amount</th>
                                    <th>Status</th>
                                    <th className="text-end">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {orders.map(order => (
                                    <tr key={order.id}>
                                        <td>
                                            <span className="text-monospace text-sm" title={order.id}>
                                                {order.id.substring(0, 8)}...
                                            </span>
                                        </td>
                                        <td>
                                            <span className="text-monospace text-sm" title={order.userId}>
                                                {order.userId.substring(0, 8)}...
                                            </span>
                                        </td>
                                        <td>{order.items?.length || 0}</td>
                                        <td>${order.totalAmount.toFixed(2)}</td>
                                        <td>{getStatusBadge(order.status)}</td>
                                        <td className="text-end">
                                            <Link to={`/admin/orders/${order.id}`} className="btn btn-sm btn-outline-primary">
                                                View Details
                                            </Link>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}
        </div>
    );
};

export default AdminOrders;
