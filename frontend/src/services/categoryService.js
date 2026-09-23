import api from './api';

let cachedCategories = null;

export const getCategories = async () => {
    if (cachedCategories) {
        return cachedCategories;
    }
    const response = await api.get('/categories');
    cachedCategories = response.data;
    return cachedCategories;
};

export const getCategoryById = async (id) => {
    const response = await api.get(`/categories/${id}`);
    return response.data;
};

export const createCategory = async (data) => {
    const response = await api.post('/categories', data);
    cachedCategories = null; // Invalidate cache
    return response.data;
};

export const updateCategory = async (id, data) => {
    const response = await api.put(`/categories/${id}`, data);
    cachedCategories = null; // Invalidate cache
    return response.data;
};

export const deleteCategory = async (id) => {
    const response = await api.delete(`/categories/${id}`);
    cachedCategories = null; // Invalidate cache
    return response.data;
};
