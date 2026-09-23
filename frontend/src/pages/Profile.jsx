import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { getProfile, updateProfile, uploadProfileImage, deleteProfileImage } from '../services/userService';

const Profile = () => {
    const { user } = useAuth();
    const [profile, setProfile] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [successMsg, setSuccessMsg] = useState('');
    
    const [isEditing, setIsEditing] = useState(false);
    const [editData, setEditData] = useState({
        firstName: '',
        lastName: '',
        phoneNumber: ''
    });

    const fileInputRef = useRef(null);

    useEffect(() => {
        fetchProfile();
    }, []);

    const fetchProfile = async () => {
        try {
            setLoading(true);
            const data = await getProfile();
            setProfile(data);
            setEditData({
                firstName: data.firstName || '',
                lastName: data.lastName || '',
                phoneNumber: data.phoneNumber || ''
            });
        } catch (err) {
            console.error("Error fetching profile", err);
            setError("Failed to load profile. Please try again later.");
        } finally {
            setLoading(false);
        }
    };

    const handleEditChange = (e) => {
        const { name, value } = e.target;
        setEditData(prev => ({ ...prev, [name]: value }));
    };

    const handleSave = async (e) => {
        e.preventDefault();
        try {
            setError(null);
            setSuccessMsg('');
            await updateProfile(editData);
            setSuccessMsg("Profile updated successfully!");
            setIsEditing(false);
            await fetchProfile(); // Refresh data
        } catch (err) {
            console.error("Error updating profile", err);
            setError("Failed to update profile.");
        }
    };

    const handleImageUpload = async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        const formData = new FormData();
        formData.append('file', file);

        try {
            setError(null);
            setSuccessMsg('');
            const data = await uploadProfileImage(formData);
            setProfile(prev => ({ ...prev, profileImageUrl: data.imageUrl }));
            setSuccessMsg("Profile picture uploaded successfully!");
        } catch (err) {
            console.error("Error uploading image", err);
            setError("Failed to upload image. Please ensure it's a valid JPG/PNG/WEBP under 5MB.");
        }
        
        // Reset file input
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const handleDeleteImage = async () => {
        if (!window.confirm("Are you sure you want to delete your profile picture?")) return;
        
        try {
            setError(null);
            setSuccessMsg('');
            await deleteProfileImage();
            setProfile(prev => ({ ...prev, profileImageUrl: null }));
            setSuccessMsg("Profile picture deleted successfully!");
        } catch (err) {
            console.error("Error deleting image", err);
            setError("Failed to delete image.");
        }
    };

    if (loading) return <div className="container mt-4">Loading profile...</div>;
    if (error && !profile) return <div className="container mt-4 alert alert-error">{error}</div>;
    if (!profile) return <div className="container mt-4 alert alert-error">Profile not found.</div>;

    const baseApiUrl = import.meta.env.VITE_API_BASE_URL || 'https://localhost:52549/api';
    const baseUrl = baseApiUrl.replace('/api', '');
    const imageUrl = profile.profileImageUrl ? (profile.profileImageUrl.startsWith('http') ? profile.profileImageUrl : `${baseUrl}${profile.profileImageUrl}`) : null;

    return (
        <div className="container profile-page mt-4" style={{ maxWidth: '600px', margin: '0 auto' }}>
            <h1 className="page-title mb-4">My Profile</h1>
            
            {error && <div className="alert alert-error mb-4">{error}</div>}
            {successMsg && <div className="alert alert-success mb-4" style={{ backgroundColor: '#d4edda', color: '#155724', padding: '10px', borderRadius: '4px' }}>{successMsg}</div>}

            <div className="card" style={{ padding: '2rem' }}>
                <div style={{ display: 'flex', alignItems: 'center', marginBottom: '2rem' }}>
                    <div style={{ marginRight: '2rem', position: 'relative' }}>
                        {imageUrl ? (
                            <img 
                                src={imageUrl} 
                                alt="Profile" 
                                style={{ width: '120px', height: '120px', borderRadius: '50%', objectFit: 'cover', border: '1px solid #ddd' }} 
                            />
                        ) : (
                            <div style={{ 
                                width: '120px', height: '120px', borderRadius: '50%', 
                                backgroundColor: '#f0f0f0', display: 'flex', alignItems: 'center', 
                                justifyContent: 'center', border: '1px solid #ddd', color: '#888', fontSize: '3rem'
                            }}>
                                {profile.firstName ? profile.firstName.charAt(0).toUpperCase() : '?'}
                            </div>
                        )}
                        <div style={{ marginTop: '1rem', display: 'flex', flexDirection: 'column', gap: '0.5rem', alignItems: 'center' }}>
                            <input 
                                type="file" 
                                accept="image/jpeg,image/png,image/webp" 
                                style={{ display: 'none' }} 
                                ref={fileInputRef}
                                onChange={handleImageUpload}
                            />
                            <button 
                                onClick={() => fileInputRef.current?.click()} 
                                className="btn btn-secondary btn-sm"
                            >
                                {imageUrl ? 'Change Picture' : 'Upload Picture'}
                            </button>
                            {imageUrl && (
                                <button 
                                    onClick={handleDeleteImage} 
                                    className="btn btn-sm" 
                                    style={{ color: '#dc3545', background: 'none', border: 'none', textDecoration: 'underline', cursor: 'pointer' }}
                                >
                                    Delete
                                </button>
                            )}
                        </div>
                    </div>

                    <div>
                        <h2>{profile.firstName} {profile.lastName}</h2>
                        <p style={{ color: '#666', marginBottom: '0.5rem' }}>{profile.email}</p>
                        <p>
                            <span style={{ backgroundColor: 'var(--primary-color)', color: 'white', padding: '0.2rem 0.5rem', borderRadius: '4px', fontSize: '0.85rem' }}>
                                {profile.role}
                            </span>
                        </p>
                    </div>
                </div>

                <hr style={{ margin: '1.5rem 0', borderColor: '#eee' }} />

                {isEditing ? (
                    <form onSubmit={handleSave}>
                        <div className="form-group mb-3">
                            <label>First Name</label>
                            <input 
                                type="text" 
                                name="firstName" 
                                className="form-control" 
                                value={editData.firstName} 
                                onChange={handleEditChange} 
                                required
                            />
                        </div>
                        <div className="form-group mb-3">
                            <label>Last Name</label>
                            <input 
                                type="text" 
                                name="lastName" 
                                className="form-control" 
                                value={editData.lastName} 
                                onChange={handleEditChange} 
                                required
                            />
                        </div>
                        <div className="form-group mb-3">
                            <label>Phone Number</label>
                            <input 
                                type="tel" 
                                name="phoneNumber" 
                                className="form-control" 
                                value={editData.phoneNumber} 
                                onChange={handleEditChange} 
                            />
                        </div>
                        <div className="form-group mb-3">
                            <label>Email (Cannot be changed)</label>
                            <input 
                                type="email" 
                                className="form-control" 
                                value={profile.email} 
                                disabled 
                                style={{ backgroundColor: '#f9f9f9', cursor: 'not-allowed' }}
                            />
                        </div>
                        <div style={{ display: 'flex', gap: '1rem', marginTop: '1.5rem' }}>
                            <button type="submit" className="btn btn-primary">Save Changes</button>
                            <button type="button" className="btn btn-secondary" onClick={() => { setIsEditing(false); setEditData({ firstName: profile.firstName, lastName: profile.lastName, phoneNumber: profile.phoneNumber || '' }); }}>Cancel</button>
                        </div>
                    </form>
                ) : (
                    <div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                            <h3>Personal Information</h3>
                            <button onClick={() => setIsEditing(true)} className="btn btn-secondary btn-sm">Edit Profile</button>
                        </div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: '1rem' }}>
                            <div style={{ fontWeight: '500', color: '#555' }}>First Name</div>
                            <div>{profile.firstName}</div>
                            
                            <div style={{ fontWeight: '500', color: '#555' }}>Last Name</div>
                            <div>{profile.lastName}</div>
                            
                            <div style={{ fontWeight: '500', color: '#555' }}>Phone Number</div>
                            <div>{profile.phoneNumber || <span style={{ color: '#aaa', fontStyle: 'italic' }}>Not provided</span>}</div>
                            
                            <div style={{ fontWeight: '500', color: '#555' }}>Email</div>
                            <div>{profile.email}</div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default Profile;
