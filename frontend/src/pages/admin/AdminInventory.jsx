import React, { useState, useEffect } from 'react';
import { 
    getInventory, 
    getLowStock, 
    updateInventory, 
    increaseStock, 
    decreaseStock 
} from '../../services/inventoryService';
import { searchProducts } from '../../services/productService';

const AdminInventory = () => {
    const [inventory, setInventory] = useState([]);
    const [products, setProducts] = useState({});
    const [viewMode, setViewMode] = useState('all'); // 'all' | 'low'
    
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionError, setActionError] = useState(null);
    const [actionSuccess, setActionSuccess] = useState(null);
    
    // Inline edit state
    const [editingId, setEditingId] = useState(null);
    const [editForm, setEditForm] = useState({ quantity: 0, reorderLevel: 0 });
    const [saving, setSaving] = useState(false);

    const fetchData = async () => {
        try {
            setLoading(true);
            setError(null);
            
            // 1. Fetch products if we haven't already (simple map resolution)
            let currentProducts = products;
            if (Object.keys(currentProducts).length === 0) {
                // Fetch up to 1000 to resolve names for this demo application
                const prodResponse = await searchProducts({ pageSize: 1000 }).catch(() => ({ data: [] }));
                const map = {};
                prodResponse.data.forEach(p => {
                    map[p.id] = { name: p.name, sku: p.sku };
                });
                setProducts(map);
                currentProducts = map;
            }

            // 2. Fetch inventory
            let invData;
            if (viewMode === 'low') {
                invData = await getLowStock();
            } else {
                invData = await getInventory();
            }
            
            setInventory(invData);
        } catch (err) {
            console.error("Failed to fetch inventory:", err);
            setError("Failed to load inventory data.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
    }, [viewMode]);

    const clearMessages = () => {
        setActionError(null);
        setActionSuccess(null);
    };

    const [adjustState, setAdjustState] = useState(null);

    const handleIncrease = (productId) => {
        clearMessages();
        setAdjustState({ productId, type: 'increase', amount: '' });
    };

    const handleDecrease = (productId) => {
        clearMessages();
        setAdjustState({ productId, type: 'decrease', amount: '' });
    };

    const submitAdjustment = async () => {
        const { productId, type, amount } = adjustState;
        const parsedAmount = parseInt(amount, 10);
        
        if (isNaN(parsedAmount) || parsedAmount <= 0) {
            alert("Please enter a valid positive number.");
            return;
        }

        try {
            setSaving(true);
            if (type === 'increase') {
                await increaseStock(productId, parsedAmount);
                setActionSuccess(`Successfully increased stock by ${parsedAmount}.`);
            } else {
                await decreaseStock(productId, parsedAmount);
                setActionSuccess(`Successfully decreased stock by ${parsedAmount}.`);
            }
            setAdjustState(null);
            fetchData();
        } catch (err) {
            const msg = err.response?.data?.message || err.response?.data?.error || `Failed to ${type} stock.`;
            setActionError(msg);
        } finally {
            setSaving(false);
        }
    };

    const startEdit = (item) => {
        clearMessages();
        setEditingId(item.productId);
        setEditForm({
            quantity: item.quantity,
            reorderLevel: item.reorderLevel
        });
    };

    const cancelEdit = () => {
        setEditingId(null);
    };

    const saveEdit = async (productId) => {
        clearMessages();
        const q = parseInt(editForm.quantity, 10);
        const r = parseInt(editForm.reorderLevel, 10);

        if (isNaN(q) || q < 0) {
            alert("Quantity must be zero or positive.");
            return;
        }
        if (isNaN(r) || r < 0) {
            alert("Reorder level must be zero or positive.");
            return;
        }

        try {
            setSaving(true);
            await updateInventory(productId, { quantity: q, reorderLevel: r });
            setActionSuccess("Inventory updated successfully.");
            setEditingId(null);
            fetchData();
        } catch (err) {
            const msg = err.response?.data?.message || err.response?.data?.error || "Failed to update inventory.";
            setActionError(msg);
        } finally {
            setSaving(false);
        }
    };

    const getStatusIndicator = (quantity, reorderLevel) => {
        if (quantity === 0) {
            return <span className="badge badge-error">Out of Stock</span>;
        }
        if (quantity <= reorderLevel) {
            return <span className="badge badge-warning">Low Stock</span>;
        }
        return <span className="badge badge-success">In Stock</span>;
    };

    return (
        <div className="container" style={{ position: 'relative' }}>
            {adjustState && (
                <div style={{
                    position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, 
                    backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1000,
                    display: 'flex', alignItems: 'center', justifyContent: 'center'
                }}>
                    <div className="card" style={{ padding: '2rem', minWidth: '300px', margin: '1rem' }}>
                        <h3 style={{ marginTop: 0 }}>
                            {adjustState.type === 'increase' ? 'Increase Stock' : 'Decrease Stock'}
                        </h3>
                        <div className="form-group" style={{ marginBottom: '1.5rem' }}>
                            <label style={{ display: 'block', marginBottom: '0.5rem' }}>Amount</label>
                            <input 
                                type="number" 
                                className="form-control" 
                                style={{ width: '100%', padding: '0.75rem' }}
                                value={adjustState.amount}
                                onChange={e => setAdjustState({...adjustState, amount: e.target.value})}
                                min="1"
                                autoFocus
                            />
                        </div>
                        <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end' }}>
                            <button 
                                className="btn btn-secondary"
                                onClick={() => setAdjustState(null)}
                                disabled={saving}
                            >
                                Cancel
                            </button>
                            <button 
                                className="btn btn-primary"
                                onClick={submitAdjustment}
                                disabled={saving}
                            >
                                {saving ? 'Saving...' : 'Confirm'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem', flexWrap: 'wrap', gap: '1rem' }}>
                <h1 style={{ margin: 0 }}>Inventory Management</h1>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                    <button 
                        className={`btn ${viewMode === 'all' ? 'btn-primary' : 'btn-secondary'}`}
                        onClick={() => setViewMode('all')}
                        disabled={loading}
                    >
                        All Inventory
                    </button>
                    <button 
                        className={`btn ${viewMode === 'low' ? 'btn-primary' : 'btn-secondary'}`}
                        onClick={() => setViewMode('low')}
                        disabled={loading}
                    >
                        Low Stock
                    </button>
                </div>
            </div>

            {actionError && (
                <div className="alert alert-error" style={{ marginBottom: '1.5rem' }}>{actionError}</div>
            )}
            {actionSuccess && (
                <div className="alert alert-success" style={{ marginBottom: '1.5rem' }}>{actionSuccess}</div>
            )}

            {error ? (
                <div className="alert alert-error">{error}</div>
            ) : loading ? (
                <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Loading inventory...</div>
            ) : inventory.length === 0 ? (
                <div className="card" style={{ padding: '3rem', textAlign: 'center' }}>
                    <p style={{ color: 'var(--text-secondary)', margin: 0 }}>No inventory records found for this view.</p>
                </div>
            ) : (
                <div className="card" style={{ padding: '0', overflowX: 'auto' }}>
                    <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                        <thead>
                            <tr style={{ borderBottom: '2px solid var(--border-color)', backgroundColor: 'rgba(0,0,0,0.02)' }}>
                                <th style={{ padding: '1rem', fontWeight: 'bold' }}>SKU</th>
                                <th style={{ padding: '1rem', fontWeight: 'bold' }}>Product</th>
                                <th style={{ padding: '1rem', fontWeight: 'bold' }}>Status</th>
                                <th style={{ padding: '1rem', fontWeight: 'bold' }}>Quantity</th>
                                <th style={{ padding: '1rem', fontWeight: 'bold' }}>Reorder Level</th>
                                <th style={{ padding: '1rem', fontWeight: 'bold', textAlign: 'right' }}>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {inventory.map(item => {
                                const prodInfo = products[item.productId] || { name: 'Unknown', sku: 'N/A' };
                                const isEditing = editingId === item.productId;

                                return (
                                    <tr key={item.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                                        <td style={{ padding: '1rem', color: 'var(--text-secondary)' }}>{prodInfo.sku}</td>
                                        <td style={{ padding: '1rem', fontWeight: '500' }}>{prodInfo.name}</td>
                                        <td style={{ padding: '1rem' }}>
                                            {!isEditing && getStatusIndicator(item.quantity, item.reorderLevel)}
                                        </td>
                                        <td style={{ padding: '1rem' }}>
                                            {isEditing ? (
                                                <input 
                                                    type="number" 
                                                    className="form-control"
                                                    style={{ width: '80px', padding: '0.25rem' }}
                                                    value={editForm.quantity}
                                                    onChange={e => setEditForm({...editForm, quantity: e.target.value})}
                                                />
                                            ) : item.quantity}
                                        </td>
                                        <td style={{ padding: '1rem' }}>
                                            {isEditing ? (
                                                <input 
                                                    type="number" 
                                                    className="form-control"
                                                    style={{ width: '80px', padding: '0.25rem' }}
                                                    value={editForm.reorderLevel}
                                                    onChange={e => setEditForm({...editForm, reorderLevel: e.target.value})}
                                                />
                                            ) : item.reorderLevel}
                                        </td>
                                        <td style={{ padding: '1rem', textAlign: 'right' }}>
                                            {isEditing ? (
                                                <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                                                    <button 
                                                        className="btn btn-primary" 
                                                        style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem' }}
                                                        onClick={() => saveEdit(item.productId)}
                                                        disabled={saving}
                                                    >
                                                        Save
                                                    </button>
                                                    <button 
                                                        className="btn btn-secondary" 
                                                        style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem' }}
                                                        onClick={cancelEdit}
                                                        disabled={saving}
                                                    >
                                                        Cancel
                                                    </button>
                                                </div>
                                            ) : (
                                                <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end', flexWrap: 'wrap' }}>
                                                    <button 
                                                        className="btn btn-secondary" 
                                                        style={{ padding: '0.25rem 0.5rem', fontSize: '0.875rem' }}
                                                        onClick={() => startEdit(item)}
                                                        title="Edit Full Inventory Record"
                                                    >
                                                        Edit
                                                    </button>
                                                    <button 
                                                        className="btn btn-secondary" 
                                                        style={{ padding: '0.25rem 0.5rem', fontSize: '0.875rem', color: 'var(--success-color)' }}
                                                        onClick={() => handleIncrease(item.productId)}
                                                    >
                                                        + Increase
                                                    </button>
                                                    <button 
                                                        className="btn btn-secondary" 
                                                        style={{ padding: '0.25rem 0.5rem', fontSize: '0.875rem', color: 'var(--error-color)' }}
                                                        onClick={() => handleDecrease(item.productId)}
                                                    >
                                                        - Decrease
                                                    </button>
                                                </div>
                                            )}
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
};

export default AdminInventory;
