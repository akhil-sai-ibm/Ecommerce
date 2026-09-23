import api from './api';

export const getProfile = async () => {
    const response = await api.get('/users/profile');
    return response.data;
};

export const updateProfile = async (profileData) => {
    const response = await api.put('/users/profile', profileData);
    return response.data;
};

export const uploadProfileImage = async (formData) => {
    const response = await api.post('/users/profile/image', formData, {
        headers: {
            'Content-Type': 'multipart/form-data',
        },
    });
    return response.data;
};

export const deleteProfileImage = async () => {
    const response = await api.delete('/users/profile/image');
    return response.data;
};
