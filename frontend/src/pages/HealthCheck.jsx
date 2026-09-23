import { useState, useEffect } from 'react';
import api from '../services/api';

const HealthCheck = () => {
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const checkBackend = async () => {
            try {
                // Testing connectivity with a public endpoint
                const response = await api.get('/products');
                setData(response.data);
                setError(null);
            } catch (err) {
                setError(err.message || 'Error connecting to backend');
            } finally {
                setLoading(false);
            }
        };

        checkBackend();
    }, []);

    return (
        <div style={{ padding: '2rem', fontFamily: 'system-ui' }}>
            <h1>Backend Connectivity Test</h1>
            
            {loading && <p>Loading data from backend...</p>}
            
            {error && (
                <div style={{ color: 'red' }}>
                    <h2>Connection Failed</h2>
                    <p>{error}</p>
                </div>
            )}
            
            {data && (
                <div style={{ color: 'green' }}>
                    <h2>Connection Successful!</h2>
                    <p>Received {data.totalCount || data.length || 0} products from the backend.</p>
                    <pre style={{ background: '#f4f4f4', padding: '1rem', color: '#333' }}>
                        {JSON.stringify(data, null, 2)}
                    </pre>
                </div>
            )}
        </div>
    );
};

export default HealthCheck;
