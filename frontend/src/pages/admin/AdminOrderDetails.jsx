import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { adminOrderService } from '../../services/adminOrderService';
import { getProductById } from '../../services/productService';

const AdminOrderDetails = () => {
    const { id } = useParams();
    const [order, setOrder] = useState(null);
    const [productNames, setProductNames] = useState({});
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState(false);
    const [error, setError] = useState(null);
    const [actionError, setActionError] = useState(null);

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

    const fetchOrder = async () => {
        try {
            setLoading(true);
            setError(null);
            
            const fetchedOrder = await adminOrderService.getOrderById(id);
            setOrder(fetchedOrder);
            
            // Best effort product name resolution
            if (fetchedOrder && fetchedOrder.items && fetchedOrder.items.length > 0) {
                resolveProductNames(fetchedOrder.items);
            }
        } catch (err) {
            console.error("Failed to load order details:", err);
            setError("Failed to load order details. It may not exist.");
        } finally {
            setLoading(false);
        }
    };

    const resolveProductNames = async (items) => {
        const names = { ...productNames };
        let hasNew = false;
        
        await Promise.all(
            items.map(async (item) => {
                if (!names[item.productId]) {
                    try {
                        const product = await getProductById(item.productId);
                        names[item.productId] = product.name;
                        hasNew = true;
                    } catch (e) {
                        names[item.productId] = 'Unknown Product';
                    }
                }
            })
        );
        
        if (hasNew) {
            setProductNames(names);
        }
    };

    useEffect(() => {
        fetchOrder();
    }, [id]);

    const handleUpdateStatus = async (newStatus) => {
        if (!window.confirm("Are you sure you want to update the status of this order?")) return;
        
        try {
            setActionLoading(true);
            setActionError(null);
            await adminOrderService.updateOrderStatus(id, newStatus);
            // Refresh order
            await fetchOrder();
        } catch (err) {
            console.error("Failed to update status:", err);
            const msg = err.response?.data?.message || err.response?.data?.error || "Failed to update order status.";
            setActionError(msg);
        } finally {
            setActionLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="container py-5 text-center">
                <div className="spinner"></div>
                <p className="mt-3 text-muted">Loading order details...</p>
            </div>
        );
    }

    if (error || !order) {
        return (
            <div className="container py-5 text-center">
                <div className="alert alert-error mx-auto" style={{ maxWidth: '600px' }}>
                    {error || "Order not found."}
                </div>
                <Link to="/admin/orders" className="btn btn-outline-primary mt-3">Back to Orders</Link>
            </div>
        );
    }

    // Determine allowed next states based on current state
    const currentStatus = order.status;
    const isTerminal = currentStatus === 4 || currentStatus === 5; // Delivered or Cancelled

    return (
        <div className="container">
            <div className="mb-4">
                <Link to="/admin/orders" className="text-muted text-decoration-none">
                    &larr; Back to Orders
                </Link>
            </div>

            <div className="d-flex align-items-center justify-content-between mb-4 flex-wrap gap-3">
                <h1 className="h3 mb-0">Order {order.id.substring(0, 8)}...</h1>
                <div>{getStatusBadge(order.status)}</div>
            </div>

            {actionError && (
                <div className="alert alert-error mb-4">
                    {actionError}
                </div>
            )}

            <div className="row">
                <div className="col-12 col-md-8">
                    <div className="card mb-4">
                        <div className="card-header border-bottom">
                            <h2 className="h5 mb-0">Order Items</h2>
                        </div>
                        <div className="table-responsive">
                            <table className="table mb-0">
                                <thead className="bg-light">
                                    <tr>
                                        <th>Product</th>
                                        <th>Price</th>
                                        <th>Qty</th>
                                        <th className="text-end">Total</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {order.items.map(item => (
                                        <tr key={item.id}>
                                            <td>
                                                <div className="fw-medium">
                                                    {productNames[item.productId] || 'Loading...'}
                                                </div>
                                                <div className="text-sm text-muted text-monospace mt-1" title={item.productId}>
                                                    ID: {item.productId.substring(0, 8)}...
                                                </div>
                                            </td>
                                            <td>${item.unitPrice.toFixed(2)}</td>
                                            <td>{item.quantity}</td>
                                            <td className="text-end fw-medium">${(item.unitPrice * item.quantity).toFixed(2)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                                <tfoot>
                                    <tr>
                                        <td colSpan="3" className="text-end fw-bold pt-3">Order Total</td>
                                        <td className="text-end fw-bold pt-3">${order.totalAmount.toFixed(2)}</td>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </div>
                </div>

                <div className="col-12 col-md-4">
                    <div className="card mb-4">
                        <div className="card-header border-bottom">
                            <h2 className="h5 mb-0">Customer Details</h2>
                        </div>
                        <div className="card-body">
                            <p className="text-sm text-muted mb-1">User ID</p>
                            <p className="text-monospace fw-medium">{order.userId}</p>
                            
                            {order.paymentId && (
                                <>
                                    <p className="text-sm text-muted mb-1 mt-3">Payment ID</p>
                                    <p className="text-monospace fw-medium">{order.paymentId}</p>
                                </>
                            )}
                        </div>
                    </div>

                    <div className="card mb-4">
                        <div className="card-header border-bottom">
                            <h2 className="h5 mb-0">Update Status</h2>
                        </div>
                        <div className="card-body">
                            {isTerminal ? (
                                <p className="text-muted text-sm mb-0">
                                    This order has reached a terminal state ({currentStatus === 4 ? 'Delivered' : 'Cancelled'}) and cannot be modified.
                                </p>
                            ) : (
                                <div className="d-flex flex-column gap-2">
                                    {currentStatus === 0 && (
                                        <button className="btn btn-outline-info w-100" onClick={() => handleUpdateStatus(1)} disabled={actionLoading}>
                                            Mark as Confirmed
                                        </button>
                                    )}
                                    {(currentStatus === 0 || currentStatus === 1) && (
                                        <button className="btn btn-outline-primary w-100" onClick={() => handleUpdateStatus(2)} disabled={actionLoading}>
                                            Mark as Processing
                                        </button>
                                    )}
                                    {currentStatus === 2 && (
                                        <button className="btn btn-outline-accent w-100" onClick={() => handleUpdateStatus(3)} disabled={actionLoading}>
                                            Mark as Shipped
                                        </button>
                                    )}
                                    {currentStatus === 3 && (
                                        <button className="btn btn-outline-success w-100" onClick={() => handleUpdateStatus(4)} disabled={actionLoading}>
                                            Mark as Delivered
                                        </button>
                                    )}
                                    
                                    <hr className="my-2" />
                                    <button className="btn btn-outline-danger w-100" onClick={() => handleUpdateStatus(5)} disabled={actionLoading}>
                                        Cancel Order
                                    </button>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default AdminOrderDetails;
