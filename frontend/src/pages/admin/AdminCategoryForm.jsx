import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { getCategoryById, createCategory, updateCategory } from '../../services/categoryService';

const AdminCategoryForm = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const isEditMode = !!id;

    const [loading, setLoading] = useState(isEditMode);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState(null);
    const [validationErrors, setValidationErrors] = useState(null);

    const [formData, setFormData] = useState({
        name: '',
        description: ''
    });

    useEffect(() => {
        const loadInitialData = async () => {
            if (!isEditMode) return;

            try {
                setLoading(true);
                setError(null);
                
                const category = await getCategoryById(id);
                setFormData({
                    name: category.name || '',
                    description: category.description || ''
                });
            } catch (err) {
                console.error("Failed to load category details:", err);
                setError("Failed to load category details.");
            } finally {
                setLoading(false);
            }
        };

        loadInitialData();
    }, [id, isEditMode]);

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        try {
            setSubmitting(true);
            setError(null);
            setValidationErrors(null);

            const payload = {
                name: formData.name.trim(),
                description: formData.description.trim() || null
            };

            if (isEditMode) {
                await updateCategory(id, payload);
            } else {
                await createCategory(payload);
            }

            navigate('/admin/categories');
        } catch (err) {
            console.error("Save failed:", err);
            if (err.response?.status === 400 && err.response?.data?.errors) {
                setValidationErrors(err.response.data.errors);
            } else {
                const msg = err.response?.data?.message || err.response?.data?.error || "Failed to save the category.";
                setError(msg);
            }
        } finally {
            setSubmitting(false);
        }
    };

    if (loading) {
        return (
            <div className="container">
                <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Loading...</div>
            </div>
        );
    }

    return (
        <div className="container" style={{ maxWidth: '600px' }}>
            <div style={{ marginBottom: '2rem' }}>
                <Link to="/admin/categories" style={{ color: 'var(--text-secondary)', textDecoration: 'none', fontSize: '0.875rem' }}>
                    &larr; Back to Categories
                </Link>
                <h1 style={{ margin: '1rem 0 0 0' }}>{isEditMode ? 'Edit Category' : 'Create Category'}</h1>
            </div>

            {error && (
                <div className="alert alert-error" style={{ marginBottom: '1.5rem' }}>{error}</div>
            )}

            <div className="card" style={{ padding: '2rem' }}>
                <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
                    <div className="form-group">
                        <label htmlFor="name" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Category Name *</label>
                        <input 
                            type="text" 
                            id="name"
                            name="name"
                            className="form-control" 
                            style={{ width: '100%', padding: '0.75rem', border: '1px solid var(--border-color)', borderRadius: '4px' }}
                            value={formData.name}
                            onChange={handleChange}
                            required
                            disabled={submitting}
                        />
                        {validationErrors?.Name && <div style={{ color: 'var(--error-color)', fontSize: '0.875rem', marginTop: '0.25rem' }}>{validationErrors.Name[0]}</div>}
                    </div>

                    <div className="form-group">
                        <label htmlFor="description" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Description</label>
                        <textarea 
                            id="description"
                            name="description"
                            className="form-control" 
                            style={{ width: '100%', padding: '0.75rem', border: '1px solid var(--border-color)', borderRadius: '4px', minHeight: '100px' }}
                            value={formData.description}
                            onChange={handleChange}
                            disabled={submitting}
                        />
                        {validationErrors?.Description && <div style={{ color: 'var(--error-color)', fontSize: '0.875rem', marginTop: '0.25rem' }}>{validationErrors.Description[0]}</div>}
                    </div>

                    <div style={{ display: 'flex', gap: '1rem', marginTop: '1rem', paddingTop: '1.5rem', borderTop: '1px solid var(--border-color)' }}>
                        <button type="submit" className="btn btn-primary" disabled={submitting}>
                            {submitting ? 'Saving...' : 'Save Category'}
                        </button>
                        <Link to="/admin/categories" className="btn btn-secondary">
                            Cancel
                        </Link>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default AdminCategoryForm;
