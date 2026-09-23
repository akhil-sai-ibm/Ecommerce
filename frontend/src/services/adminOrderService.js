import api from './api';

const API_URL = '/orders';

export const adminOrderService = {
  getOrdersByUser: async (userId) => {
    const response = await api.get(`${API_URL}/user/${userId}`);
    return response.data;
  },

  getOrderById: async (orderId) => {
    const response = await api.get(`${API_URL}/${orderId}`);
    return response.data;
  },

  updateOrderStatus: async (orderId, status) => {
    const response = await api.put(`${API_URL}/${orderId}/status?status=${status}`);
    return response.data;
  }
};
