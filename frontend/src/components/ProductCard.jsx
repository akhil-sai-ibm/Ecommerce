import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { addCartItem } from '../services/cartService';


const getStatusBadge = (status) => {
    switch (status) {
        case 0:
            return <span className="badge badge-success">Active</span>;
        case 1:
            return <span className="badge badge-warning">Inactive</span>;
        case 2:
            return <span className="badge badge-error">Out of Stock</span>;
        case 3:
            return <span className="badge badge-neutral">Discontinued</span>;
        default:
            return null;
    }
};

const ProductCard = ({ product }) => {
    const [addingToCart, setAddingToCart] = useState(false);

    const handleAddToCart = async (e) => {
        e.preventDefault();
        setAddingToCart(true);
        try {
            await addCartItem(product.id, 1);
            alert('Added to cart successfully.');
        } catch (err) {
            const errorMsg = err.response?.data?.message || err.response?.data?.error || 'Failed to add item to cart.';
            alert(errorMsg);
        } finally {
            setAddingToCart(false);
        }
    };

    const baseUrl = import.meta.env.VITE_API_BASE_URL?.replace('/api', '') || 'https://localhost:52549';
    const imageUrl = product.imageUrl ? (product.imageUrl.startsWith('http') ? product.imageUrl : `${baseUrl}${product.imageUrl}`) : null;

    return (
        <div className="card product-card">
            <div className="product-image-placeholder">
                {imageUrl ? (
                    <img src={imageUrl} alt={product.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                ) : (
                    <span>No Image</span>
                )}
            </div>
            <div className="product-content">
                <div className="product-header">
                    <h3 className="product-title" title={product.name}>
                        {product.name}
                    </h3>
                    <div className="product-status">
                        {getStatusBadge(product.status)}
                    </div>
                </div>
                <div className="product-sku">SKU: {product.sku}</div>
                <div className="product-stock text-sm text-secondary mb-2">
                    {product.stockQuantity > 0 ? `${product.stockQuantity} in stock` : 'Out of stock'}
                </div>
                <div className="product-price">
                    ${product.price.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </div>
                <div className="product-actions" style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
                    <button 
                        className="btn btn-primary" 
                        style={{ flex: 1 }}
                        disabled={product.status !== 0 || addingToCart || product.stockQuantity <= 0}
                        onClick={handleAddToCart}
                    >
                        {addingToCart ? 'Adding...' : 'Add to Cart'}
                    </button>
                    <Link to={`/products/${product.id}`} className="btn btn-secondary" style={{ flex: 1 }}>
                        Details
                    </Link>
                </div>
            </div>
        </div>
    );
};

export default ProductCard;
