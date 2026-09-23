import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getProductById } from '../services/productService';
import { getCategories } from '../services/categoryService';
import { addCartItem } from '../services/cartService';


const getStatusBadge = (status) => {
    switch (status) {
        case 0: return <span className="badge badge-success">Active</span>;
        case 1: return <span className="badge badge-warning">Inactive</span>;
        case 2: return <span className="badge badge-error">Out of Stock</span>;
        case 3: return <span className="badge badge-neutral">Discontinued</span>;
        default: return null;
    }
};

const ProductDetails = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const [product, setProduct] = useState(null);
    const [categoryName, setCategoryName] = useState('');
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [addingToCart, setAddingToCart] = useState(false);
    const [actionMessage, setActionMessage] = useState(null); // { type: 'success' | 'error', text: string }
    const [quantity, setQuantity] = useState(1);

    useEffect(() => {
        const fetchDetails = async () => {
            try {
                const productData = await getProductById(id);
                setProduct(productData);
                
                // Fetch category name
                const categories = await getCategories();
                const category = categories.find(c => c.id === productData.categoryId);
                if (category) {
                    setCategoryName(category.name);
                }
            } catch (err) {
                setError('Failed to load product details.');
                console.error(err);
            } finally {
                setLoading(false);
            }
        };

        fetchDetails();
    }, [id]);

    if (loading) {
        return <div className="container"><div className="loading-state">Loading product details...</div></div>;
    }

    if (error || !product) {
        return (
            <div className="container">
                <div className="alert alert-error">{error || 'Product not found.'}</div>
                <button className="btn btn-secondary" onClick={() => navigate('/products')}>Back to Products</button>
            </div>
        );
    }

    const baseUrl = import.meta.env.VITE_API_BASE_URL?.replace('/api', '') || 'https://localhost:52549';
    const imageUrl = product.imageUrl ? (product.imageUrl.startsWith('http') ? product.imageUrl : `${baseUrl}${product.imageUrl}`) : null;

    return (
        <div className="container product-details-page">
            <button className="btn btn-secondary back-button" onClick={() => navigate('/products')}>
                &larr; Back to Products
            </button>

            <div className="product-details-container card">
                <div className="product-details-image">
                    {imageUrl ? (
                        <img src={imageUrl} alt={product.name} style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: '4px' }} />
                    ) : (
                        <span>No Image Available</span>
                    )}
                </div>
                <div className="product-details-info">
                    <div className="product-details-header">
                        <h1 className="product-details-title">{product.name}</h1>
                        <div className="product-details-status">
                            {getStatusBadge(product.status)}
                        </div>
                    </div>
                    
                    <div className="product-meta">
                        <span className="product-meta-item">SKU: {product.sku}</span>
                        {categoryName && <span className="product-meta-item">Category: {categoryName}</span>}
                    </div>
                    
                    <div className="product-stock text-sm mt-2" style={{ color: product.stockQuantity > 0 ? 'inherit' : 'var(--error-color)' }}>
                        {product.stockQuantity > 0 ? `${product.stockQuantity} in stock` : 'Out of stock'}
                    </div>

                    <div className="product-details-price">
                        ${product.price.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </div>

                    <div className="product-description-block">
                        <h3>Description</h3>
                        <p className="product-description">{product.description || 'No description available.'}</p>
                    </div>

                    <div className="product-actions-block">
                        {actionMessage && (
                            <div className={`alert alert-${actionMessage.type}`} style={{ marginBottom: '1rem' }}>
                                {actionMessage.text}
                                {actionMessage.type === 'success' && (
                                    <div style={{ marginTop: '0.5rem' }}>
                                        <button className="btn btn-secondary" onClick={() => navigate('/cart')} style={{ marginRight: '0.5rem', padding: '0.25rem 0.5rem', fontSize: '0.875rem' }}>View Cart</button>
                                        <button className="btn btn-secondary" onClick={() => setActionMessage(null)} style={{ padding: '0.25rem 0.5rem', fontSize: '0.875rem' }}>Continue Shopping</button>
                                    </div>
                                )}
                            </div>
                        )}
                        
                        {product.status === 0 && product.stockQuantity > 0 && (
                            <div className="quantity-selector" style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                <label htmlFor="quantity">Quantity:</label>
                                <select 
                                    id="quantity" 
                                    value={quantity} 
                                    onChange={(e) => setQuantity(Number(e.target.value))}
                                    className="input"
                                    style={{ width: '80px', padding: '0.25rem 0.5rem' }}
                                >
                                    {[...Array(Math.min(10, product.stockQuantity)).keys()].map(i => (
                                        <option key={i + 1} value={i + 1}>{i + 1}</option>
                                    ))}
                                </select>
                            </div>
                        )}

                        <button 
                            className="btn btn-primary btn-large w-full"
                            disabled={product.status !== 0 || addingToCart || product.stockQuantity <= 0} // Only allow adding to cart if Active and in stock
                            onClick={async () => {
                                setActionMessage(null);
                                setAddingToCart(true);
                                try {
                                    await addCartItem(product.id, quantity);
                                    setActionMessage({ type: 'success', text: 'Added to cart successfully.' });
                                } catch (err) {
                                    const errorMsg = err.response?.data?.message || err.response?.data?.error || 'Failed to add item to cart.';
                                    setActionMessage({ type: 'error', text: errorMsg });
                                } finally {
                                    setAddingToCart(false);
                                }
                            }}
                        >
                            {addingToCart ? 'Adding...' : 'Add to Cart'}
                        </button>
                        {(product.status !== 0 || product.stockQuantity <= 0) && (
                            <p className="status-note text-secondary mt-2 text-sm">
                                This product is currently unavailable for purchase.
                            </p>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ProductDetails;
