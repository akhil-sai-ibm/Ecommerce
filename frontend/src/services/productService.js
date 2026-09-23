import api from './api';

export const searchProducts = async (params) => {
    const response = await api.get('/products/search', { params });
    return {
        data: response.data,
        totalCount: parseInt(response.headers['x-total-count'] || '0', 10)
    };
};

export const getProductById = async (id) => {
    const response = await api.get(`/products/${id}`);
    return response.data;
};

export const bulkUploadProducts = async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    
    // Axios will automatically set the correct Content-Type for multipart/form-data
    const response = await api.post('/products/bulk-upload', formData);
    return response.data;
};

export const createProduct = async (data) => {
    const response = await api.post('/products', data);
    return response.data;
};

export const updateProduct = async (id, data) => {
    const response = await api.put(`/products/${id}`, data);
    return response.data;
};

export const deleteProduct = async (id) => {
    const response = await api.delete(`/products/${id}`);
    return response.data;
};

export const uploadProductImage = async (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    
    const response = await api.post(`/products/${id}/image`, formData, {
        headers: {
            'Content-Type': 'multipart/form-data',
        },
    });
    return response.data;
};

export const deleteProductImage = async (id) => {
    const response = await api.delete(`/products/${id}/image`);
    return response.data;
};
