import api from './api';
import { v4 as uuidv4 } from 'uuid';

export const getPaymentByOrderId = async (orderId) => {
    try {
        const response = await api.get(`/payments/order/${orderId}`);
        return response.data || null;
    } catch (error) {
        if (error.response && error.response.status === 404) {
            // No payment found for this order
            return null;
        }
        throw error;
    }
};

export const createPayment = async (data) => {
    const response = await api.post('/payments', data, {
        headers: {
            'Idempotency-Key': uuidv4()
        }
    });
    return response.data;
};

export const processPayment = async (paymentId) => {
    const response = await api.post(`/payments/${paymentId}/process`);
    return response.data;
};
