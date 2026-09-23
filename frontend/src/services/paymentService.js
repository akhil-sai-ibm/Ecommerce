import api from './api';

export const getPaymentByOrderId = async (orderId) => {
    try {
        const response = await api.get(`/payments/order/${orderId}`);
        return response.data;
    } catch (error) {
        if (error.response && error.response.status === 404) {
            // No payment found for this order
            return null;
        }
        throw error;
    }
};

export const createPayment = async (data) => {
    const response = await api.post('/payments', data);
    return response.data;
};

export const processPayment = async (paymentId) => {
    const response = await api.post(`/payments/${paymentId}/process`);
    return response.data;
};
