import React, { useState, useEffect } from 'react';
import { searchProducts } from '../services/productService';
import { getCategories } from '../services/categoryService';
import ProductCard from '../components/ProductCard';
import Pagination from '../components/Pagination';

const Products = () => {
    const [products, setProducts] = useState([]);
    const [categories, setCategories] = useState([]);
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Filters and Pagination State
    const [searchTerm, setSearchTerm] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [sortBy, setSortBy] = useState('name');
    const [sortDirection, setSortDirection] = useState('asc');
    const [pageNumber, setPageNumber] = useState(1);
    const pageSize = 12;

    useEffect(() => {
        const fetchCategories = async () => {
            try {
                const data = await getCategories();
                setCategories(data);
            } catch (err) {
                console.error("Failed to load categories", err);
            }
        };
        fetchCategories();
    }, []);

    useEffect(() => {
        const fetchProducts = async () => {
            setLoading(true);
            setError(null);
            try {
                const params = {
                    pageNumber,
                    pageSize,
                    sortBy,
                    sortDirection
                };
                if (searchTerm) params.q = searchTerm;
                if (categoryId) params.categoryId = categoryId;

                const result = await searchProducts(params);
                setProducts(result.data);
                setTotalCount(result.totalCount);
            } catch (err) {
                setError('Failed to load products. Please try again later.');
                console.error(err);
            } finally {
                setLoading(false);
            }
        };

        // Debounce search slightly
        const timeoutId = setTimeout(() => {
            fetchProducts();
        }, 300);

        return () => clearTimeout(timeoutId);
    }, [searchTerm, categoryId, sortBy, sortDirection, pageNumber]);

    const handleSearchChange = (e) => {
        setSearchTerm(e.target.value);
        setPageNumber(1);
    };

    const handleCategoryChange = (e) => {
        setCategoryId(e.target.value);
        setPageNumber(1);
    };

    const handleSortChange = (e) => {
        const [newSortBy, newSortDirection] = e.target.value.split('-');
        setSortBy(newSortBy);
        setSortDirection(newSortDirection);
        setPageNumber(1);
    };

    return (
        <div className="container products-page">
            <h1 className="page-title">Product Catalog</h1>

            <div className="filters-section card">
                <div className="filter-group">
                    <label htmlFor="search">Search</label>
                    <input
                        type="text"
                        id="search"
                        placeholder="Search products..."
                        value={searchTerm}
                        onChange={handleSearchChange}
                    />
                </div>
                
                <div className="filter-group">
                    <label htmlFor="category">Category</label>
                    <select id="category" value={categoryId} onChange={handleCategoryChange} className="form-select">
                        <option value="">All Categories</option>
                        {categories.map(c => (
                            <option key={c.id} value={c.id}>{c.name}</option>
                        ))}
                    </select>
                </div>

                <div className="filter-group">
                    <label htmlFor="sort">Sort By</label>
                    <select id="sort" value={`${sortBy}-${sortDirection}`} onChange={handleSortChange} className="form-select">
                        <option value="name-asc">Name (A-Z)</option>
                        <option value="name-desc">Name (Z-A)</option>
                        <option value="price-asc">Price (Low to High)</option>
                        <option value="price-desc">Price (High to Low)</option>
                        <option value="createdat-desc">Newest First</option>
                        <option value="createdat-asc">Oldest First</option>
                    </select>
                </div>
            </div>

            {error && <div className="alert alert-error">{error}</div>}

            <div className="products-grid-container">
                {loading ? (
                    <div className="loading-state">Loading products...</div>
                ) : products.length === 0 ? (
                    <div className="empty-state card">
                        <h3>No products found</h3>
                        <p>Try adjusting your search or filters.</p>
                    </div>
                ) : (
                    <>
                        <div className="products-grid">
                            {products.map(product => (
                                <ProductCard key={product.id} product={product} />
                            ))}
                        </div>
                        <div className="pagination-wrapper">
                            <Pagination
                                currentPage={pageNumber}
                                totalCount={totalCount}
                                pageSize={pageSize}
                                onPageChange={setPageNumber}
                            />
                            <div className="total-results">
                                Showing {products.length} of {totalCount} products
                            </div>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
};

export default Products;
