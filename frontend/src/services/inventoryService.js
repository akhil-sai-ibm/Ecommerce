import api from './api';

export const getInventory = async () => {
    const response = await api.get('/inventory');
    return response.data;
};

export const getLowStock = async () => {
    const response = await api.get('/inventory/low');
    return response.data;
};

export const updateInventory = async (productId, data) => {
    const response = await api.put(`/inventory/${productId}`, data);
    return response.data;
};

export const increaseStock = async (productId, amount) => {
    const response = await api.post(`/inventory/${productId}/increase?amount=${amount}`);
    return response.data;
};

export const decreaseStock = async (productId, amount) => {
    const response = await api.post(`/inventory/${productId}/decrease?amount=${amount}`);
    return response.data;
};
