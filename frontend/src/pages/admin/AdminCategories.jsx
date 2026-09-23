import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getCategories, deleteCategory } from '../../services/categoryService';

const AdminCategories = () => {
    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionError, setActionError] = useState(null);
    const [actionSuccess, setActionSuccess] = useState(null);

    const fetchData = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await getCategories();
            setCategories(data);
        } catch (err) {
            console.error("Failed to fetch categories:", err);
            setError("Failed to load categories. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
    }, []);

    const handleDelete = async (id, name) => {
        if (!window.confirm(`Are you sure you want to delete category "${name}"?\nThis action cannot be undone.`)) {
            return;
        }

        try {
            setActionError(null);
            setActionSuccess(null);
            await deleteCategory(id);
            setActionSuccess(`Category "${name}" was deleted successfully.`);
            fetchData();
        } catch (err) {
            console.error("Delete failed:", err);
            const msg = err.response?.data?.message || err.response?.data?.error || "Failed to delete the category. It may be in use by products.";
            setActionError(msg);
        }
    };

    return (
        <div className="container">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem', flexWrap: 'wrap', gap: '1rem' }}>
                <h1 style={{ margin: 0 }}>Categories</h1>
                <Link to="/admin/categories/new" className="btn btn-primary">
                    + New Category
                </Link>
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
                <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Loading categories...</div>
            ) : categories.length === 0 ? (
                <div className="card" style={{ padding: '3rem', textAlign: 'center' }}>
                    <p style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }}>No categories found.</p>
                    <Link to="/admin/categories/new" className="btn btn-primary">Create the first category</Link>
                </div>
            ) : (
                <div className="card" style={{ padding: '0', overflowX: 'auto' }}>
                    <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                        <thead>
                            <tr style={{ borderBottom: '2px solid var(--border-color)', backgroundColor: 'rgba(0,0,0,0.02)' }}>
                                <th style={{ padding: '1rem', fontWeight: 'bold' }}>Name</th>
                                <th style={{ padding: '1rem', fontWeight: 'bold' }}>Description</th>
                                <th style={{ padding: '1rem', fontWeight: 'bold', textAlign: 'right' }}>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {categories.map(category => (
                                <tr key={category.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                                    <td style={{ padding: '1rem', fontWeight: '500' }}>{category.name}</td>
                                    <td style={{ padding: '1rem', color: 'var(--text-secondary)' }}>
                                        {category.description || <span style={{ fontStyle: 'italic', opacity: 0.5 }}>No description</span>}
                                    </td>
                                    <td style={{ padding: '1rem', textAlign: 'right' }}>
                                        <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                                            <Link 
                                                to={`/admin/categories/${category.id}/edit`} 
                                                className="btn btn-secondary" 
                                                style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem' }}
                                            >
                                                Edit
                                            </Link>
                                            <button 
                                                onClick={() => handleDelete(category.id, category.name)} 
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
            )}
        </div>
    );
};

export default AdminCategories;
