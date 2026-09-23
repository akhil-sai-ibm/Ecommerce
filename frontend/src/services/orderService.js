import api from './api';

export const checkout = async (idempotencyKey, requestDto) => {
    const config = idempotencyKey ? {
        headers: {
            'Idempotency-Key': idempotencyKey
        }
    } : {};
    
    const response = await api.post('/orders/checkout', requestDto || {}, config);
    return response.data; // returns OrderDto
};

export const getOrders = async () => {
    const response = await api.get('/orders/me');
    return response.data;
};

export const getOrderById = async (id) => {
    const response = await api.get(`/orders/${id}`);
    return response.data;
};

export const cancelOrder = async (id) => {
    const response = await api.post(`/orders/${id}/cancel`);
    return response.data;
};
