import React, { useState, useRef } from 'react';
import { Link } from 'react-router-dom';
import { bulkUploadProducts } from '../../services/productService';

const AdminBulkUpload = () => {
    const [file, setFile] = useState(null);
    const [uploading, setUploading] = useState(false);
    
    // Result states
    const [result, setResult] = useState(null);
    const [error, setError] = useState(null);
    
    const fileInputRef = useRef(null);

    const handleFileChange = (e) => {
        if (e.target.files && e.target.files.length > 0) {
            const selectedFile = e.target.files[0];
            const ext = selectedFile.name.split('.').pop().toLowerCase();
            
            if (ext !== 'xlsx') {
                setError("Only .xlsx files are supported.");
                setFile(null);
                e.target.value = '';
                return;
            }
            
            setFile(selectedFile);
            setError(null);
            setResult(null);
        }
    };

    const handleUpload = async () => {
        if (!file) return;

        try {
            setUploading(true);
            setError(null);
            setResult(null);
            
            const response = await bulkUploadProducts(file);
            setResult(response);
        } catch (err) {
            console.error("Upload failed:", err);
            const msg = err.response?.data?.message || err.response?.data?.error || "Failed to upload file. Please try again.";
            setError(msg);
        } finally {
            setUploading(false);
        }
    };

    const handleReset = () => {
        setFile(null);
        setResult(null);
        setError(null);
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    return (
        <div className="container" style={{ maxWidth: '800px' }}>
            <div style={{ marginBottom: '2rem' }}>
                <Link to="/admin/products" style={{ color: 'var(--text-secondary)', textDecoration: 'none', fontSize: '0.875rem' }}>
                    &larr; Back to Products
                </Link>
                <h1 style={{ margin: '1rem 0 0 0' }}>Bulk Upload Products</h1>
                <p style={{ color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
                    Upload an Excel (.xlsx) file to create multiple products at once.
                </p>
            </div>

            {!result && (
                <div className="card" style={{ padding: '2rem', marginBottom: '2rem' }}>
                    <div style={{ marginBottom: '1.5rem' }}>
                        <h3 style={{ margin: '0 0 0.5rem 0' }}>Expected File Format</h3>
                        <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: '1rem' }}>
                            The Excel file must include a header row. The following columns are supported:
                        </p>
                        <div style={{ backgroundColor: 'rgba(0,0,0,0.03)', padding: '1rem', borderRadius: '4px', fontFamily: 'monospace', fontSize: '0.875rem' }}>
                            Name*, Description, Price*, SKU*, Category*, Status, Quantity*, ReorderLevel*
                        </div>
                        <p style={{ color: 'var(--text-secondary)', fontSize: '0.75rem', marginTop: '0.5rem' }}>
                            * Indicates a required field. Price, Quantity, and ReorderLevel must be zero or positive.
                        </p>
                    </div>

                    <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '1.5rem' }}>
                        <div className="form-group">
                            <label htmlFor="file-upload" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
                                Select Excel File (.xlsx)
                            </label>
                            <input
                                type="file"
                                id="file-upload"
                                accept=".xlsx"
                                onChange={handleFileChange}
                                ref={fileInputRef}
                                disabled={uploading}
                                className="form-control"
                                style={{ width: '100%', padding: '0.5rem', border: '1px solid var(--border-color)', borderRadius: '4px' }}
                            />
                        </div>

                        {file && (
                            <div style={{ marginTop: '1rem', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                                Selected: <strong>{file.name}</strong> ({(file.size / 1024).toFixed(2)} KB)
                            </div>
                        )}

                        {error && (
                            <div className="alert alert-error" style={{ marginTop: '1rem' }}>
                                {error}
                            </div>
                        )}

                        <div style={{ display: 'flex', gap: '1rem', marginTop: '1.5rem' }}>
                            <button
                                className="btn btn-primary"
                                onClick={handleUpload}
                                disabled={!file || uploading}
                            >
                                {uploading ? 'Uploading products...' : 'Upload File'}
                            </button>
                            {file && !uploading && (
                                <button className="btn btn-secondary" onClick={handleReset}>
                                    Clear
                                </button>
                            )}
                        </div>
                    </div>
                </div>
            )}

            {result && (
                <div>
                    {result.successfulRows > 0 && result.failedRows === 0 && (
                        <div className="alert alert-success" style={{ marginBottom: '1.5rem' }}>
                            Upload completed successfully!
                        </div>
                    )}
                    
                    {result.successfulRows > 0 && result.failedRows > 0 && (
                        <div className="alert alert-warning" style={{ marginBottom: '1.5rem' }}>
                            Upload completed with some errors.
                        </div>
                    )}

                    {result.successfulRows === 0 && result.failedRows > 0 && (
                        <div className="alert alert-error" style={{ marginBottom: '1.5rem' }}>
                            Upload failed. All rows contained errors.
                        </div>
                    )}

                    <div className="card" style={{ padding: '1.5rem', marginBottom: '2rem' }}>
                        <h3 style={{ margin: '0 0 1rem 0' }}>Upload Summary</h3>
                        <div style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
                            <div>
                                <span style={{ color: 'var(--text-secondary)', display: 'block', fontSize: '0.875rem' }}>Total Rows</span>
                                <strong style={{ fontSize: '1.25rem' }}>{result.totalRows}</strong>
                            </div>
                            <div>
                                <span style={{ color: 'var(--text-secondary)', display: 'block', fontSize: '0.875rem' }}>Successful</span>
                                <strong style={{ fontSize: '1.25rem', color: 'var(--success-color)' }}>{result.successfulRows}</strong>
                            </div>
                            <div>
                                <span style={{ color: 'var(--text-secondary)', display: 'block', fontSize: '0.875rem' }}>Failed</span>
                                <strong style={{ fontSize: '1.25rem', color: 'var(--error-color)' }}>{result.failedRows}</strong>
                            </div>
                            <div>
                                <span style={{ color: 'var(--text-secondary)', display: 'block', fontSize: '0.875rem' }}>Batches Processed</span>
                                <strong style={{ fontSize: '1.25rem' }}>{result.batchesProcessed}</strong>
                            </div>
                        </div>
                    </div>

                    {result.errors && result.errors.length > 0 && (
                        <div className="card" style={{ padding: '0', overflowX: 'auto' }}>
                            <div style={{ padding: '1rem', borderBottom: '1px solid var(--border-color)', backgroundColor: 'rgba(0,0,0,0.02)' }}>
                                <h3 style={{ margin: 0, fontSize: '1rem' }}>Row Errors</h3>
                            </div>
                            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                                <thead>
                                    <tr style={{ borderBottom: '2px solid var(--border-color)' }}>
                                        <th style={{ padding: '1rem', fontWeight: 'bold', width: '80px' }}>Row</th>
                                        <th style={{ padding: '1rem', fontWeight: 'bold', width: '150px' }}>SKU</th>
                                        <th style={{ padding: '1rem', fontWeight: 'bold' }}>Error Details</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {result.errors.map((err, idx) => (
                                        <tr key={idx} style={{ borderBottom: '1px solid var(--border-color)' }}>
                                            <td style={{ padding: '1rem', color: 'var(--text-secondary)' }}>{err.rowNumber}</td>
                                            <td style={{ padding: '1rem', fontWeight: '500' }}>{err.sku || '-'}</td>
                                            <td style={{ padding: '1rem', color: 'var(--error-color)' }}>{err.error}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}

                    <div style={{ marginTop: '2rem', display: 'flex', gap: '1rem' }}>
                        <Link to="/admin/products" className="btn btn-primary">
                            View Products
                        </Link>
                        <button className="btn btn-secondary" onClick={handleReset}>
                            Upload Another File
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default AdminBulkUpload;
