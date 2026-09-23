import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { getProductById, createProduct, updateProduct, uploadProductImage, deleteProductImage } from '../../services/productService';
import { getCategories } from '../../services/categoryService';

const AdminProductForm = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const isEditMode = !!id;

    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(isEditMode);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState(null);
    const [validationErrors, setValidationErrors] = useState(null);

    const [formData, setFormData] = useState({
        name: '',
        description: '',
        price: '',
        sku: '',
        status: 0,
        categoryId: '',
        imageUrl: ''
    });
    
    const [imageFile, setImageFile] = useState(null);

    useEffect(() => {
        const loadInitialData = async () => {
            try {
                setLoading(true);
                setError(null);
                
                const cats = await getCategories().catch(() => []);
                setCategories(cats);

                if (isEditMode) {
                    const product = await getProductById(id);
                    setFormData({
                        name: product.name || '',
                        description: product.description || '',
                        price: product.price?.toString() || '',
                        sku: product.sku || '',
                        status: product.status ?? 0,
                        categoryId: product.categoryId || '',
                        imageUrl: product.imageUrl || ''
                    });
                } else if (cats.length > 0) {
                    // Default to first category for new products
                    setFormData(prev => ({ ...prev, categoryId: cats[0].id }));
                }
            } catch (err) {
                console.error("Failed to load initial data:", err);
                setError(isEditMode ? "Failed to load product details." : "Failed to load categories.");
            } finally {
                setLoading(false);
            }
        };

        loadInitialData();
    }, [id, isEditMode]);

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({
            ...prev,
            [name]: name === 'status' ? parseInt(value, 10) : value
        }));
    };

    const handleImageChange = (e) => {
        if (e.target.files && e.target.files[0]) {
            setImageFile(e.target.files[0]);
        }
    };

    const handleDeleteImage = async () => {
        if (!window.confirm("Are you sure you want to delete this product's image?")) return;
        
        try {
            setSubmitting(true);
            setError(null);
            await deleteProductImage(id);
            setFormData(prev => ({ ...prev, imageUrl: '' }));
            setImageFile(null);
        } catch (err) {
            console.error("Error deleting image", err);
            setError("Failed to delete image.");
        } finally {
            setSubmitting(false);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        try {
            setSubmitting(true);
            setError(null);
            setValidationErrors(null);

            const payload = {
                ...formData,
                price: parseFloat(formData.price)
            };

            if (isNaN(payload.price) || payload.price < 0) {
                setValidationErrors({ Price: ["Price must be a valid positive number."] });
                setSubmitting(false);
                return;
            }

            let productId = id;
            if (isEditMode) {
                await updateProduct(id, payload);
            } else {
                const createdProduct = await createProduct(payload);
                productId = createdProduct.id;
            }

            if (imageFile && productId) {
                try {
                    await uploadProductImage(productId, imageFile);
                } catch (imgErr) {
                    console.error("Image upload failed:", imgErr);
                    // It's not a complete failure if the product saved but the image failed,
                    // but we should probably warn the user.
                    if (!isEditMode) {
                        // If we are creating, we should probably delete the product if the image upload fails?
                        // Or just let it be. We'll just show an error.
                        setError("Product saved but image upload failed.");
                        setSubmitting(false);
                        return;
                    }
                }
            }

            // Successfully saved, go back to product list
            navigate('/admin/products');
        } catch (err) {
            console.error("Save failed:", err);
            if (err.response?.status === 400 && err.response?.data?.errors) {
                setValidationErrors(err.response.data.errors);
            } else {
                const msg = err.response?.data?.message || err.response?.data?.error || "Failed to save the product. Please check your inputs.";
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
        <div className="container" style={{ maxWidth: '800px' }}>
            <div style={{ marginBottom: '2rem' }}>
                <Link to="/admin/products" style={{ color: 'var(--text-secondary)', textDecoration: 'none', fontSize: '0.875rem' }}>
                    &larr; Back to Products
                </Link>
                <h1 style={{ margin: '1rem 0 0 0' }}>{isEditMode ? 'Edit Product' : 'Create Product'}</h1>
            </div>

            {error && (
                <div className="alert alert-error" style={{ marginBottom: '1.5rem' }}>{error}</div>
            )}

            <div className="card" style={{ padding: '2rem' }}>
                <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
                    <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap' }}>
                        <div className="form-group" style={{ flex: '1 1 300px' }}>
                            <label htmlFor="name" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Product Name *</label>
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

                        <div className="form-group" style={{ flex: '1 1 200px' }}>
                            <label htmlFor="sku" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>SKU *</label>
                            <input 
                                type="text" 
                                id="sku"
                                name="sku"
                                className="form-control" 
                                style={{ width: '100%', padding: '0.75rem', border: '1px solid var(--border-color)', borderRadius: '4px' }}
                                value={formData.sku}
                                onChange={handleChange}
                                required
                                disabled={submitting}
                            />
                            {validationErrors?.SKU && <div style={{ color: 'var(--error-color)', fontSize: '0.875rem', marginTop: '0.25rem' }}>{validationErrors.SKU[0]}</div>}
                        </div>
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

                    <div className="form-group">
                        <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Product Image</label>
                        
                        {formData.imageUrl && !imageFile && (
                            <div style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
                                <img 
                                    src={formData.imageUrl.startsWith('http') ? formData.imageUrl : `${import.meta.env.VITE_API_BASE_URL?.replace('/api', '') || 'https://localhost:52549'}${formData.imageUrl}`} 
                                    alt="Product" 
                                    style={{ width: '150px', height: '150px', objectFit: 'cover', border: '1px solid #ddd', borderRadius: '4px' }} 
                                />
                                <button type="button" className="btn btn-secondary btn-sm" onClick={handleDeleteImage} disabled={submitting}>
                                    Delete Image
                                </button>
                            </div>
                        )}
                        
                        <input 
                            type="file" 
                            accept="image/jpeg,image/png,image/webp" 
                            className="form-control" 
                            style={{ width: '100%', padding: '0.75rem', border: '1px solid var(--border-color)', borderRadius: '4px' }}
                            onChange={handleImageChange}
                            disabled={submitting}
                        />
                        {imageFile && <div style={{ fontSize: '0.875rem', marginTop: '0.25rem', color: 'var(--success-color)' }}>New image selected: {imageFile.name}</div>}
                    </div>

                    <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap' }}>
                        <div className="form-group" style={{ flex: '1 1 200px' }}>
                            <label htmlFor="price" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Price (USD) *</label>
                            <input 
                                type="number" 
                                id="price"
                                name="price"
                                step="0.01"
                                min="0"
                                className="form-control" 
                                style={{ width: '100%', padding: '0.75rem', border: '1px solid var(--border-color)', borderRadius: '4px' }}
                                value={formData.price}
                                onChange={handleChange}
                                required
                                disabled={submitting}
                            />
                            {validationErrors?.Price && <div style={{ color: 'var(--error-color)', fontSize: '0.875rem', marginTop: '0.25rem' }}>{validationErrors.Price[0]}</div>}
                        </div>

                        <div className="form-group" style={{ flex: '1 1 200px' }}>
                            <label htmlFor="categoryId" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Category *</label>
                            <select 
                                id="categoryId"
                                name="categoryId"
                                className="form-control" 
                                style={{ width: '100%', padding: '0.75rem', border: '1px solid var(--border-color)', borderRadius: '4px', backgroundColor: 'var(--bg-color)' }}
                                value={formData.categoryId}
                                onChange={handleChange}
                                required
                                disabled={submitting}
                            >
                                <option value="" disabled>Select a category</option>
                                {categories.map(cat => (
                                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                                ))}
                            </select>
                            {validationErrors?.CategoryId && <div style={{ color: 'var(--error-color)', fontSize: '0.875rem', marginTop: '0.25rem' }}>{validationErrors.CategoryId[0]}</div>}
                        </div>

                        <div className="form-group" style={{ flex: '1 1 200px' }}>
                            <label htmlFor="status" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>Status *</label>
                            <select 
                                id="status"
                                name="status"
                                className="form-control" 
                                style={{ width: '100%', padding: '0.75rem', border: '1px solid var(--border-color)', borderRadius: '4px', backgroundColor: 'var(--bg-color)' }}
                                value={formData.status}
                                onChange={handleChange}
                                required
                                disabled={submitting}
                            >
                                <option value={0}>Active</option>
                                <option value={1}>Inactive</option>
                                <option value={2}>Out Of Stock</option>
                                <option value={3}>Discontinued</option>
                            </select>
                            {validationErrors?.Status && <div style={{ color: 'var(--error-color)', fontSize: '0.875rem', marginTop: '0.25rem' }}>{validationErrors.Status[0]}</div>}
                        </div>
                    </div>

                    <div style={{ display: 'flex', gap: '1rem', marginTop: '1rem', paddingTop: '1.5rem', borderTop: '1px solid var(--border-color)' }}>
                        <button type="submit" className="btn btn-primary" disabled={submitting}>
                            {submitting ? 'Saving...' : 'Save Product'}
                        </button>
                        <Link to="/admin/products" className="btn btn-secondary">
                            Cancel
                        </Link>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default AdminProductForm;
