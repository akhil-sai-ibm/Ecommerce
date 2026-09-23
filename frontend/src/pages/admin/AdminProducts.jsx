import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { searchProducts, deleteProduct } from '../../services/productService';
import { getCategories } from '../../services/categoryService';

const AdminProducts = () => {
    const [products, setProducts] = useState([]);
    const [categories, setCategories] = useState({});
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionError, setActionError] = useState(null);
    const [actionSuccess, setActionSuccess] = useState(null);
    
    // Pagination & Search
    const [page, setPage] = useState(1);
    const [pageSize] = useState(10);
    const [searchQuery, setSearchQuery] = useState('');

    const getStatusBadge = (status) => {
        switch (status) {
            case 0: return <span className="badge badge-success">Active</span>;
            case 1: return <span className="badge badge-neutral">Inactive</span>;
            case 2: return <span className="badge badge-warning">Out Of Stock</span>;
            case 3: return <span className="badge badge-error">Discontinued</span>;
            default: return <span className="badge badge-neutral">Unknown</span>;
        }
    };

    const fetchData = async () => {
        try {
            setLoading(true);
            setError(null);
            
            // Fetch categories mapping if empty
            let cats = categories;
            if (Object.keys(categories).length === 0) {
                const catsResponse = await getCategories().catch(() => []);
                const map = {};
                catsResponse.forEach(c => map[c.id] = c.name);
                setCategories(map);
                cats = map;
            }

            const response = await searchProducts({
                pageNumber: page,
                pageSize: pageSize,
                q: searchQuery || undefined
            });

            setProducts(response.data);
            setTotalCount(response.totalCount);
        } catch (err) {
            console.error("Failed to fetch products:", err);
            setError("Failed to load products. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
    }, [page]); // Re-fetch when page changes

    const handleSearchSubmit = (e) => {
        e.preventDefault();
        setPage(1);
        if (page === 1) {
            fetchData();
        }
    };

    const handleDelete = async (id, name) => {
        if (!window.confirm(`Are you sure you want to delete "${name}"?\nThis action cannot be undone.`)) {
            return;
        }

        try {
            setActionError(null);
            setActionSuccess(null);
            const result = await deleteProduct(id);
            const successMsg = result?.message || `Product "${name}" was deleted successfully.`;
            setActionSuccess(successMsg);
            
            // If deleting the last item on a page > 1, go back a page
            if (products.length === 1 && page > 1) {
                setPage(page - 1);
            } else {
                fetchData();
            }
        } catch (err) {
            console.error("Delete failed:", err);
            const msg = err.response?.data?.message || err.response?.data?.error || "Failed to delete the product.";
            setActionError(msg);
        }
    };

    const totalPages = Math.ceil(totalCount / pageSize);

    return (
        <div className="container">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem', flexWrap: 'wrap', gap: '1rem' }}>
                <h1 style={{ margin: 0 }}>Products</h1>
                <div style={{ display: 'flex', gap: '1rem' }}>
                    <Link to="/admin/products/bulk-upload" className="btn btn-secondary">
                        Bulk Upload
                    </Link>
                    <Link to="/admin/products/new" className="btn btn-primary">
                        + New Product
                    </Link>
                </div>
            </div>

            {actionError && (
                <div className="alert alert-error" style={{ marginBottom: '1.5rem' }}>{actionError}</div>
            )}
            {actionSuccess && (
                <div className="alert alert-success" style={{ marginBottom: '1.5rem' }}>{actionSuccess}</div>
            )}

            <div className="card" style={{ padding: '1.5rem', marginBottom: '1.5rem' }}>
                <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                    <input 
                        type="text" 
                        className="form-control" 
                        placeholder="Search products by name or SKU..." 
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        style={{ flex: '1', minWidth: '200px' }}
                    />
                    <button type="submit" className="btn btn-secondary" disabled={loading}>
                        Search
                    </button>
                    {searchQuery && (
                        <button type="button" className="btn btn-secondary" onClick={() => { setSearchQuery(''); setPage(1); }} disabled={loading}>
                            Clear
                        </button>
                    )}
                </form>
            </div>

            {error ? (
                <div className="alert alert-error">{error}</div>
            ) : loading ? (
                <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Loading products...</div>
            ) : products.length === 0 ? (
                <div className="card" style={{ padding: '3rem', textAlign: 'center' }}>
                    <p style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }}>No products found.</p>
                    <Link to="/admin/products/new" className="btn btn-primary">Create the first product</Link>
                </div>
            ) : (
                <>
                    <div className="card" style={{ padding: '0', overflowX: 'auto' }}>
                        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                            <thead>
                                <tr style={{ borderBottom: '2px solid var(--border-color)', backgroundColor: 'rgba(0,0,0,0.02)' }}>
                                    <th style={{ padding: '1rem', fontWeight: 'bold' }}>SKU</th>
                                    <th style={{ padding: '1rem', fontWeight: 'bold' }}>Name</th>
                                    <th style={{ padding: '1rem', fontWeight: 'bold' }}>Category</th>
                                    <th style={{ padding: '1rem', fontWeight: 'bold' }}>Price</th>
                                    <th style={{ padding: '1rem', fontWeight: 'bold' }}>Status</th>
                                    <th style={{ padding: '1rem', fontWeight: 'bold', textAlign: 'right' }}>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {products.map(product => (
                                    <tr key={product.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                                        <td style={{ padding: '1rem', color: 'var(--text-secondary)' }}>{product.sku}</td>
                                        <td style={{ padding: '1rem', fontWeight: '500' }}>{product.name}</td>
                                        <td style={{ padding: '1rem' }}>{categories[product.categoryId] || 'Unknown'}</td>
                                        <td style={{ padding: '1rem' }}>${product.price.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                                        <td style={{ padding: '1rem' }}>{getStatusBadge(product.status)}</td>
                                        <td style={{ padding: '1rem', textAlign: 'right' }}>
                                            <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                                                <Link 
                                                    to={`/admin/products/${product.id}/edit`} 
                                                    className="btn btn-secondary" 
                                                    style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem' }}
                                                >
                                                    Edit
                                                </Link>
                                                <button 
                                                    onClick={() => handleDelete(product.id, product.name)} 
                                                    className="btn btn-secondary" 
                                                    style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem', color: 'var(--error-color)', borderColor: 'var(--border-color)' }}
                                                >
                                                    Delete
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    {totalPages > 1 && (
                        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '1rem', marginTop: '2rem' }}>
                            <button 
                                className="btn btn-secondary" 
                                disabled={page === 1} 
                                onClick={() => setPage(page - 1)}
                            >
                                Previous
                            </button>
                            <span style={{ color: 'var(--text-secondary)' }}>
                                Page {page} of {totalPages}
                            </span>
                            <button 
                                className="btn btn-secondary" 
                                disabled={page >= totalPages} 
                                onClick={() => setPage(page + 1)}
                            >
                                Next
                            </button>
                        </div>
                    )}
                </>
            )}
        </div>
    );
};

export default AdminProducts;
